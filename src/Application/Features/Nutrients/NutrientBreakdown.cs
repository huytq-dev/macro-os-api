namespace Application;

/// <summary>Danh sách chất kèm mức hiệu lực và % so target, dùng cho màn chi tiết món/entry (F10).</summary>
public static class NutrientBreakdown
{
    /// <summary>
    /// Chất core luôn có mặt (Amount null nếu chưa có dữ liệu); chất chi tiết của USDA chỉ có khi món/entry có dữ liệu chất đó.
    /// Target/floor/ceiling là mức hiệu lực của <paramref name="date"/>. Thêm <c>carbs_net_g</c> sau fiber khi biết carbs.
    /// </summary>
    public static async Task<List<NutrientAmountModel>> BuildAsync(IUnitOfWork uow, Guid userId, DateOnly date,
        IReadOnlyDictionary<string, decimal> amounts, CancellationToken ct)
    {
        var catalog = await NutrientCatalog.BuildAsync(uow, userId, date, withAmounts: false, ct, includeExtended: true);

        var items = new List<NutrientAmountModel>(catalog.Count + 1);
        foreach (var n in catalog)
        {
            var has = amounts.TryGetValue(n.Code, out var value);
            if (!n.IsCore && !has) continue;
            items.Add(ToModel(n.Code, n.Name, n.Unit, n.Group, n.DisplayOrder, n.DisplayPrecision, has ? value : null, n.Floor, n.Target, n.Ceiling, n.IsCore, n.GoalKind));

            if (n.Code == "fiber_g" && amounts.TryGetValue("carbs_g", out var carbs))
                items.Add(ToModel(NutrientCatalog.NetCarbsCode, "Net (Non-fiber)", "g", nameof(NutrientGroup.Carb), n.DisplayOrder + 5, 1,
                    NutritionCalculator.NetCarbs(carbs, has ? value : null), null, null, null, true, NutrientGoals.None));
        }
        return items;
    }

    private static NutrientAmountModel ToModel(string code, string name, string unit, string group, int order, int precision,
        decimal? amount, decimal? floor, decimal? target, decimal? ceiling, bool isCore, string goalKind)
        => new(code, name, unit, group, order, precision, amount, floor, target, ceiling,
            NutritionCalculator.PercentOfTarget(amount, target), isCore,
            code == NutrientCatalog.NetCarbsCode ? NutritionOverviewKeys.NetCarbs : NutritionOverviewKeys.ByCode.GetValueOrDefault(code), goalKind);
}
