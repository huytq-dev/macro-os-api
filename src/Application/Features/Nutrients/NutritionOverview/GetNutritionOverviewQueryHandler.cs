namespace Application;

public sealed class GetNutritionOverviewQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetNutritionOverviewQuery, NutritionOverviewResponse>
{
    public async Task<NutritionOverviewResponse> Handle(GetNutritionOverviewQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await _clock.GetAsync(ct)).Today;   // ngày local của user: hôm nay/tương lai = CURRENT_ACTIVE/PLANNED (đếm vào trung bình), quá khứ theo provenance lịch sử
        var to = request.Date ?? today;
        var days = request.Range switch
        {
            OverviewRange.Week => 7,
            OverviewRange.Month => 30,
            OverviewRange.Quarter => 90,
            OverviewRange.Year => 365,
            _ => 1,
        };
        var from = to.AddDays(-(days - 1));
        var isToday = request.Range == OverviewRange.Today;

        // D2: ngày Fasting hợp lệ là ngày log 0 kcal / 0 g P-F-C và VÀO trung bình macro (như Energy Balance/Macros/Nutrient Detail); ngày chưa log vắng ⇒ không vào mẫu số
        var logged = await FoodLogSupport.LoadDaysAsync(_uow, userId, from, to, ct, includeFasting: true);
        // Vi chất chỉ có ở ngày có MÓN: mẫu số của chúng = số ngày có món (ngày nhịn ăn / chỉ-manual không có dữ liệu vi chất, không kéo trung bình xuống 0)
        var divisorDays = isToday ? 1 : logged.Values.Count(d => d.HasEntries);

        // Target hiệu lực (user ghi đè / mặc định / macro từ program của ngày `to`) và tổng lượng cả kỳ
        var catalog = await NutrientCatalog.BuildAsync(_uow, userId, to, withAmounts: false, ct);
        // Kỳ dài đọc NutritionDayAggregate (một dòng/ngày) thay vì quét mọi entry và snapshot chất
        var sums = request.Range is OverviewRange.Month or OverviewRange.Quarter or OverviewRange.Year
            ? await NutritionAggregates.SumsAsync(_uow, userId, from, to, logged.Values.Where(d => d.HasEntries).Select(d => d.Date).ToList(), _timeProvider, ct)   // ngày chỉ có manual không có aggregate
            : await NutrientCatalog.SumsAsync(_uow, userId, from, to, ct);

        // Calories/P/C/F lấy từ dinh dưỡng HIỆU LỰC của từng ngày (manual thắng Food Log, không cộng), mẫu số TỪNG TRƯỜNG = số ngày có giá trị đó (null không bao giờ thành 0);
        // vi chất vẫn từ các món Food Log. Không có override trong kỳ thì kết quả đúng bằng cách tính cũ (mọi ngày đều biết cả bốn trường)
        var macroDivisors = new Dictionary<string, int>();
        foreach (var (code, pick) in new (string Code, Func<FoodLogSupport.LoggedDay, decimal?> Pick)[]
                 { ("energy_kcal", d => d.Calories), ("protein_g", d => d.Protein), ("carbs_g", d => d.Carbs), ("fat_g", d => d.Fat) })
        {
            var known = logged.Values.Select(pick).Where(v => v.HasValue).Select(v => v!.Value).ToList();
            sums[code] = known.Count == 0 ? null : known.Sum();
            macroDivisors[code] = isToday ? 1 : known.Count;
        }
        var anyOverride = logged.Values.Any(d => d.HasOverride);

        // Kỳ dài: target macro = trung bình target các ngày đã log (program đổi tuần / dịch kcal theo ngày)
        // Chỉ ngày có target VERIFIED vào trung bình; ngày LEGACY_UNVERIFIED bị loại và đếm vào UnverifiedTargetDays (không dùng target chưa xác minh cho thống kê kỳ dài)
        var macroTargets = new Dictionary<string, decimal?>();
        var unverifiedDays = 0;
        var noVerifiedTarget = false;
        string? todayProvenance = null;
        if (!isToday)
        {
            var targets = await FoodLogSupport.ProgramTargetsAsync(_uow, userId, logged.Count > 0 ? logged.Keys.ToList() : [to], ct, today);
            var verified = targets.Where(t => t.TargetProvenance != ProgramTargetResolver.LegacyUnverified).ToList();
            unverifiedDays = targets.Count - verified.Count;
            noVerifiedTarget = targets.Count > 0 && verified.Count == 0;
            if (verified.Count > 0)
            {
                macroTargets["energy_kcal"] = NutritionRounding.Round((decimal)verified.Average(t => t.CaloriesTarget), NutritionRounding.CompactKcal);
                macroTargets["protein_g"] = NutritionRounding.Round(verified.Average(t => t.ProteinTarget), NutritionRounding.CompactMacro);
                macroTargets["carbs_g"] = NutritionRounding.Round(verified.Average(t => t.CarbsTarget), NutritionRounding.CompactMacro);
                macroTargets["fat_g"] = NutritionRounding.Round(verified.Average(t => t.FatTarget), NutritionRounding.CompactMacro);
            }
        }
        else todayProvenance = (await FoodLogSupport.TargetForDateAsync(_uow, userId, to, ct, today))?.TargetProvenance;

        // Vi chất có lịch sử ghi đè: kỳ dài dùng mức trung bình của từng ngày đã log (mỗi ngày mức CỦA NGÀY ĐÓ), nhất quán với /nutrients/{code}/series
        var averagedGoals = isToday ? [] : await NutrientCatalog.AveragedGoalsAsync(_uow, userId, logged.Keys.ToList(), to, ct);

        decimal? Avg(string code) => NutritionCalculator.AveragePerLoggedDayOrNull(sums.GetValueOrDefault(code), macroDivisors.GetValueOrDefault(code, divisorDays));

        var items = new List<NutritionOverviewItem>(catalog.Count + 1);
        foreach (var n in catalog)
        {
            if (!NutritionOverviewKeys.ByCode.TryGetValue(n.Code, out var key)) continue;
            var target = macroTargets.TryGetValue(n.Code, out var avgTarget) ? avgTarget : n.Target;
            var (floor, ceiling, goalKind) = (n.Floor, n.Ceiling, n.GoalKind);
            if (averagedGoals.TryGetValue(n.Code, out var avgGoal)) (floor, target, ceiling, goalKind) = (avgGoal.Floor, avgGoal.Target, avgGoal.Ceiling, avgGoal.Kind);
            // Macro có target trung bình theo ngày (kỳ dài) → Target; còn lại loại mục tiêu hiệu lực của catalog
            else if (macroTargets.ContainsKey(n.Code)) goalKind = NutrientGoals.KindOf(null, target, null, NutrientCatalog.Basis.Program);
            var isMacro = NutrientCatalog.MacroCodes.Contains(n.Code);
            if (isMacro && noVerifiedTarget) { target = null; goalKind = NutrientGoals.None; }   // kỳ dài mà không ngày nào VERIFIED: không đưa target chưa xác minh vào số tổng hợp
            var provenance = !isMacro || target is null ? null : isToday ? todayProvenance : ProgramTargetResolver.Verified;
            items.Add(new NutritionOverviewItem(key, Avg(n.Code), target, floor, ceiling, n.Unit, goalKind, provenance, isMacro ? unverifiedDays : 0));
            if (n.Code == "fiber_g")
                // Net carbs trộn carbs với fiber: khi kỳ có override, carbs là manual còn fiber chỉ từ món Food Log ⇒ không ghép khác nguồn, để null
                items.Add(new NutritionOverviewItem(NutritionOverviewKeys.NetCarbs, !anyOverride && Avg("carbs_g") is { } avgCarbs ? NutritionCalculator.NetCarbs(avgCarbs, Avg("fiber_g")) : null, null, Unit: "g", GoalKind: NutrientGoals.None));
        }

        // Ngày có manual override: calories/macro là số manual nên các món không được tính ⇒ không liệt kê chúng như đóng góp (cùng quy tắc với Nutrient Detail Today); không có override thì như cũ
        var contributors = !isToday ? null : anyOverride ? new NutritionContributors([], [], [], []) : await ContributorsAsync(userId, to, ct);
        return new NutritionOverviewResponse(request.Range.ToString().ToLowerInvariant(), from, to, logged.Count, items, contributors);
    }

    private async Task<NutritionContributors> ContributorsAsync(Guid userId, DateOnly date, CancellationToken ct)
    {
        var rows = await _uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodLog.UserId == userId && e.FoodLog.LogDate == date)
            .Select(e => new
            {
                Name = e.FoodItem != null ? e.FoodItem.Name : e.Recipe != null ? e.Recipe.Name : "",
                e.Calories, e.Protein, e.Fat, e.Carbs,
            })
            .ToListAsync(ct);

        return new NutritionContributors(
            TopThree(rows, r => r.Name, r => r.Calories),
            TopThree(rows, r => r.Name, r => r.Protein),
            TopThree(rows, r => r.Name, r => r.Fat),
            TopThree(rows, r => r.Name, r => r.Carbs));
    }

    /// <summary>Gộp theo tên món (cùng món log nhiều lần cộng lại), top 3 giá trị &gt; 0; tên rỗng (quick add không tên) → "Quick add".</summary>
    private static List<NutritionContributor> TopThree<T>(IEnumerable<T> rows, Func<T, string> name, Func<T, decimal> value)
        => rows.GroupBy(name)
            .Select(g => new NutritionContributor(g.Key.Length == 0 ? "Quick add" : g.Key, NutritionRounding.Round(g.Sum(value), NutritionRounding.CompactMacro)))
            .Where(c => c.Value > 0)
            .OrderByDescending(c => c.Value)
            .Take(3)
            .ToList();
}
