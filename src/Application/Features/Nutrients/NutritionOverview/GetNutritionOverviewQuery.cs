namespace Application;

public enum OverviewRange { Today, Week, Month, Quarter, Year }

/// <summary>Mã nutrient của BE (NutrientDefinition.Code) → khoá camelCase của FE trang Nutrition Overview.</summary>
public static class NutritionOverviewKeys
{
    public const string NetCarbs = "netCarbs";

    public static readonly IReadOnlyDictionary<string, string> ByCode = new Dictionary<string, string>
    {
        ["energy_kcal"] = "calories", ["protein_g"] = "protein", ["fat_g"] = "fat", ["carbs_g"] = "carbs",
        ["fiber_g"] = "fiber", ["starch_g"] = "starch", ["sugars_g"] = "sugars", ["sugars_added_g"] = "sugarsAdded",
        ["fat_monounsaturated_g"] = "monounsaturated", ["fat_polyunsaturated_g"] = "polyunsaturated",
        ["omega3_g"] = "omega3", ["omega3_ala_g"] = "omega3Ala", ["omega3_dha_g"] = "omega3Dha", ["omega3_epa_g"] = "omega3Epa",
        ["omega6_g"] = "omega6", ["fat_saturated_g"] = "saturated", ["fat_trans_g"] = "transFat",
        ["cysteine_g"] = "cysteine", ["histidine_g"] = "histidine", ["isoleucine_g"] = "isoleucine", ["leucine_g"] = "leucine",
        ["lysine_g"] = "lysine", ["methionine_g"] = "methionine", ["phenylalanine_g"] = "phenylalanine", ["threonine_g"] = "threonine",
        ["tryptophan_g"] = "tryptophan", ["tyrosine_g"] = "tyrosine", ["valine_g"] = "valine",
        ["vitamin_b1_mg"] = "b1", ["vitamin_b2_mg"] = "b2", ["vitamin_b3_mg"] = "b3", ["vitamin_b5_mg"] = "b5",
        ["vitamin_b6_mg"] = "b6", ["vitamin_b12_mcg"] = "b12", ["folate_mcg"] = "folate",
        ["vitamin_a_mcg"] = "vitaminA", ["vitamin_c_mg"] = "vitaminC", ["vitamin_d_mcg"] = "vitaminD",
        ["vitamin_e_mg"] = "vitaminE", ["vitamin_k_mcg"] = "vitaminK",
        ["calcium_mg"] = "calcium", ["copper_mg"] = "copper", ["iron_mg"] = "iron", ["magnesium_mg"] = "magnesium",
        ["manganese_mg"] = "manganese", ["phosphorus_mg"] = "phosphorus", ["potassium_mg"] = "potassium",
        ["selenium_mcg"] = "selenium", ["sodium_mg"] = "sodium", ["zinc_mg"] = "zinc",
        ["alcohol_g"] = "alcohol", ["caffeine_mg"] = "caffeine", ["cholesterol_mg"] = "cholesterol", ["choline_mg"] = "choline",
        ["water_g"] = "water",
    };
}

/// <summary>
/// UC-F07: tổng quan dinh dưỡng theo kỳ (today/week/month/quarter/year, kết thúc ở Date, mặc định hôm nay).
/// Today: tổng ngày + top 3 món đóng góp; kỳ khác: trung bình mỗi ngày đã log, target macro = trung bình target các ngày đã log.
/// Month/quarter/year đọc từ NutritionDayAggregate (WP5).
/// </summary>
public sealed record GetNutritionOverviewQuery(OverviewRange Range = OverviewRange.Today, DateOnly? Date = null)
    : IRequest<NutritionOverviewResponse>;

public sealed class GetNutritionOverviewQueryValidator : AbstractValidator<GetNutritionOverviewQuery>
{
    public GetNutritionOverviewQueryValidator()
    {
        RuleFor(x => x.Range).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.Date)
            .LessThanOrEqualTo(_ => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)))
            .WithMessage(ValidationMessages.NotInFuture)
            .When(x => x.Date.HasValue);
    }
}

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

        decimal? Avg(string code) => NutritionCalculator.AveragePerLoggedDayOrNull(sums.GetValueOrDefault(code), divisorDays);

        var items = new List<NutritionOverviewItem>(catalog.Count + 1);
        foreach (var n in catalog)
        {
            if (!NutritionOverviewKeys.ByCode.TryGetValue(n.Code, out var key)) continue;
            var target = macroTargets.TryGetValue(n.Code, out var avgTarget) ? avgTarget : n.Target;
            // Macro có target trung bình theo ngày (kỳ dài) → Target; còn lại loại mục tiêu hiệu lực của catalog
            var goalKind = macroTargets.ContainsKey(n.Code) ? NutrientGoals.KindOf(null, target, null, NutrientCatalog.Basis.Program) : n.GoalKind;
            items.Add(new NutritionOverviewItem(key, Avg(n.Code), target, n.Floor, n.Ceiling, n.Unit, goalKind));
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
