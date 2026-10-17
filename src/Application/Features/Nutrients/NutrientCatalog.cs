namespace Application;

public static class NutrientCatalog
{
    /// <summary>Target của bốn code này đến từ program (MacroProgramWeek), không cho user ghi đè ở UserNutrientTarget.</summary>
    public static readonly string[] MacroCodes = ["energy_kcal", "protein_g", "carbs_g", "fat_g"];

    public const string NetCarbsCode = "carbs_net_g";

    /// <summary>Danh sách nutrient kèm target hiệu lực + pin của user; withAmounts = tính lượng đã ăn của <paramref name="date"/>.</summary>
    public static async Task<List<NutrientModel>> BuildAsync(IUnitOfWork uow, Guid userId, DateOnly date, bool withAmounts, CancellationToken ct)
    {
        var defs = await uow.Repository<NutrientDefinition>().Query().OrderBy(d => d.DisplayOrder).ToListAsync(ct);
        var mine = (await uow.Repository<UserNutrientTarget>().Query().Where(t => t.UserId == userId).ToListAsync(ct))
            .ToDictionary(t => t.NutrientDefinitionId);
        var macro = await FoodLogSupport.TargetForDateAsync(uow, userId, date, ct);
        var amounts = withAmounts ? await AmountsAsync(uow, userId, date, defs, ct) : [];

        var items = new List<NutrientModel>(defs.Count + 1);
        foreach (var d in defs)
        {
            mine.TryGetValue(d.Id, out var t);
            var floor = t?.Floor ?? d.ReferenceMin;
            var target = t?.Target ?? d.ReferenceTarget;
            var ceiling = t?.Ceiling ?? d.ReferenceMax;
            var custom = t is not null && (t.Floor.HasValue || t.Target.HasValue || t.Ceiling.HasValue);

            if (macro is not null && MacroCodes.Contains(d.Code))
            {
                floor = null;
                ceiling = null;
                target = d.Code switch { "energy_kcal" => macro.CaloriesTarget, "protein_g" => macro.ProteinTarget, "carbs_g" => macro.CarbsTarget, _ => macro.FatTarget };
                custom = false;
            }

            amounts.TryGetValue(d.Code, out var amount);
            items.Add(new NutrientModel(d.Code, d.Name, d.Unit, d.Group.ToString(), d.DisplayOrder, d.DisplayPrecision, amount, floor, target, ceiling, custom, t?.PinOrder));

            if (withAmounts && d.Code == "fiber_g")
            {
                var net = Math.Max((amounts.GetValueOrDefault("carbs_g") ?? 0) - (amount ?? 0), 0);
                items.Add(new NutrientModel(NetCarbsCode, "Net (Non-fiber)", "g", nameof(NutrientGroup.Carb), d.DisplayOrder + 5, 1, net, null, null, null, false, null));
            }
        }
        return items;
    }

    private static async Task<Dictionary<string, decimal?>> AmountsAsync(IUnitOfWork uow, Guid userId, DateOnly date, List<NutrientDefinition> defs, CancellationToken ct)
    {
        // Entry đã xoá (soft) bị query filter loại; macro + fiber + sodium đọc từ cột snapshot của entry
        var entries = await uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodLog.UserId == userId && e.FoodLog.LogDate == date)
            .Select(e => new { e.Calories, e.Protein, e.Carbs, e.Fat, e.Fiber, e.Sodium })
            .ToListAsync(ct);

        var sums = await uow.Repository<FoodLogEntryNutrient>().Query()
            .Where(n => n.FoodLogEntry.FoodLog.UserId == userId && n.FoodLogEntry.FoodLog.LogDate == date && !n.FoodLogEntry.IsDeleted)
            .GroupBy(n => n.NutrientDefinitionId)
            .Select(g => new { Id = g.Key, Sum = g.Sum(n => n.Amount) })
            .ToListAsync(ct);
        var byId = sums.ToDictionary(s => s.Id, s => s.Sum);

        var water = await uow.Repository<FoodLog>().Query()
            .Where(l => l.UserId == userId && l.LogDate == date)
            .Select(l => (decimal?)l.TotalWater)
            .FirstOrDefaultAsync(ct) ?? 0;

        var result = new Dictionary<string, decimal?>
        {
            ["energy_kcal"] = entries.Sum(e => e.Calories),
            ["protein_g"] = entries.Sum(e => e.Protein),
            ["carbs_g"] = entries.Sum(e => e.Carbs),
            ["fat_g"] = entries.Sum(e => e.Fat),
            ["fiber_g"] = entries.Sum(e => e.Fiber ?? 0),
            ["sodium_mg"] = entries.Sum(e => e.Sodium ?? 0),
        };
        // null = chưa có dữ liệu nutrient này trong ngày (khác 0)
        foreach (var d in defs.Where(d => !NutrientSnapshot.ColumnCodes.Contains(d.Code)))
            result[d.Code] = byId.GetValueOrDefault(d.Id);
        // Nước uống (WaterLog, ml ≈ g) cộng vào water_g
        if (water > 0) result["water_g"] = (result.GetValueOrDefault("water_g") ?? 0) + water;

        return result;
    }
}
