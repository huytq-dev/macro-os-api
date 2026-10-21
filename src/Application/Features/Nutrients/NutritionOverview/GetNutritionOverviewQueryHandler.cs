namespace Application;

public sealed class GetNutritionOverviewQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetNutritionOverviewQuery, NutritionOverviewResponse>
{
    public async Task<NutritionOverviewResponse> Handle(GetNutritionOverviewQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var to = request.Date ?? FoodLogSupport.Today(_timeProvider);
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

        var logged = await FoodLogSupport.LoadDaysAsync(_uow, userId, from, to, ct);
        var divisorDays = isToday ? 1 : logged.Count;

        // Target hiệu lực (user ghi đè / mặc định / macro từ program của ngày `to`) và tổng lượng cả kỳ
        var catalog = await NutrientCatalog.BuildAsync(_uow, userId, to, withAmounts: false, ct);
        // Kỳ dài đọc NutritionDayAggregate (một dòng/ngày) thay vì quét mọi entry và snapshot chất
        var sums = request.Range is OverviewRange.Month or OverviewRange.Quarter or OverviewRange.Year
            ? await NutritionAggregates.SumsAsync(_uow, userId, from, to, logged.Keys.ToList(), _timeProvider, ct)
            : await NutrientCatalog.SumsAsync(_uow, userId, from, to, ct);

        // Kỳ dài: target macro = trung bình target các ngày đã log (program đổi tuần / dịch kcal theo ngày)
        var macroTargets = new Dictionary<string, decimal?>();
        if (!isToday)
        {
            var targets = await FoodLogSupport.ProgramTargetsAsync(_uow, userId, logged.Count > 0 ? logged.Keys.ToList() : [to], ct);
            if (targets.Count > 0)
            {
                macroTargets["energy_kcal"] = NutritionRounding.Round((decimal)targets.Average(t => t.CaloriesTarget), NutritionRounding.CompactKcal);
                macroTargets["protein_g"] = NutritionRounding.Round(targets.Average(t => t.ProteinTarget), NutritionRounding.CompactMacro);
                macroTargets["carbs_g"] = NutritionRounding.Round(targets.Average(t => t.CarbsTarget), NutritionRounding.CompactMacro);
                macroTargets["fat_g"] = NutritionRounding.Round(targets.Average(t => t.FatTarget), NutritionRounding.CompactMacro);
            }
        }

        // Vi chất có lịch sử ghi đè: kỳ dài dùng mức trung bình của từng ngày đã log (mỗi ngày mức CỦA NGÀY ĐÓ), nhất quán với /nutrients/{code}/series
        var averagedGoals = isToday ? [] : await NutrientCatalog.AveragedGoalsAsync(_uow, userId, logged.Keys.ToList(), to, ct);

        decimal? Avg(string code) => NutritionCalculator.AveragePerLoggedDayOrNull(sums.GetValueOrDefault(code), divisorDays);

        var items = new List<NutritionOverviewItem>(catalog.Count + 1);
        foreach (var n in catalog)
        {
            if (!NutritionOverviewKeys.ByCode.TryGetValue(n.Code, out var key)) continue;
            var target = macroTargets.TryGetValue(n.Code, out var avgTarget) ? avgTarget : n.Target;
            var (floor, ceiling, goalKind) = (n.Floor, n.Ceiling, n.GoalKind);
            if (averagedGoals.TryGetValue(n.Code, out var avgGoal)) (floor, target, ceiling, goalKind) = (avgGoal.Floor, avgGoal.Target, avgGoal.Ceiling, avgGoal.Kind);
            // Macro có target trung bình theo ngày (kỳ dài) → Target; còn lại loại mục tiêu hiệu lực của catalog
            else if (macroTargets.ContainsKey(n.Code)) goalKind = NutrientGoals.KindOf(null, target, null, NutrientCatalog.Basis.Program);
            items.Add(new NutritionOverviewItem(key, Avg(n.Code), target, floor, ceiling, n.Unit, goalKind));
            if (n.Code == "fiber_g")
                items.Add(new NutritionOverviewItem(NutritionOverviewKeys.NetCarbs, Avg("carbs_g") is { } avgCarbs ? NutritionCalculator.NetCarbs(avgCarbs, Avg("fiber_g")) : null, null, Unit: "g", GoalKind: NutrientGoals.None));
        }

        var contributors = isToday ? await ContributorsAsync(userId, to, ct) : null;
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
