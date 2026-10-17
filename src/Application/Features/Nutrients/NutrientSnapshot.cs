namespace Application;

/// <summary>
/// Snapshot nutrient chuẩn hoá của entry (ADR-008), tính từ FoodNutrient (/100 g) và các cột micro cũ của FoodItem.
/// Calories/P/C/F, fiber, sodium nằm ở cột FoodLogEntry nên không ghi vào FoodLogEntryNutrients.
/// </summary>
public static class NutrientSnapshot
{
    public static readonly string[] ColumnCodes = ["energy_kcal", "protein_g", "carbs_g", "fat_g", "fiber_g", "sodium_mg"];

    // Cột micro cũ của FoodItem → code chuẩn hoá; FoodNutrient thắng khi cả hai cùng có
    private static IEnumerable<(string Code, decimal? Per100g)> Legacy(FoodItem f) =>
    [
        ("sugars_g", f.SugarPer100g), ("potassium_mg", f.PotassiumPer100g), ("calcium_mg", f.CalciumPer100g),
        ("iron_mg", f.IronPer100g), ("vitamin_c_mg", f.VitaminCPer100g), ("vitamin_d_mcg", f.VitaminDPer100g),
        ("fat_saturated_g", f.SaturatedFatPer100g), ("fat_trans_g", f.TransFatPer100g), ("cholesterol_mg", f.CholesterolPer100g),
    ];

    /// <summary>Ghi snapshot cho entry: cập nhật tại chỗ dòng đã có, thêm dòng mới, xoá dòng không còn (an toàn với unique (entry, nutrient)).</summary>
    public static async Task WriteAsync(IUnitOfWork uow, Guid entryId, FoodItem food, decimal grams, CancellationToken ct)
    {
        var codeToId = (await uow.Repository<NutrientDefinition>().Query().Select(d => new { d.Id, d.Code }).ToListAsync(ct))
            .ToDictionary(d => d.Code, d => d.Id);

        var rows = await uow.Repository<FoodNutrient>().Query()
            .Where(n => n.FoodItemId == food.Id && n.AmountPer100g != null)
            .Select(n => new { n.Nutrient.Code, Amount = n.AmountPer100g, n.AmountStatus })
            .ToListAsync(ct);
        var per100 = rows.ToDictionary(r => r.Code, r => (Amount: r.Amount!.Value, Status: r.AmountStatus));
        foreach (var (code, value) in Legacy(food))
            if (value is { } v && !per100.ContainsKey(code)) per100[code] = (v, NutrientAmountStatus.Measured);

        var k = grams / 100m;
        var repo = uow.Repository<FoodLogEntryNutrient>();
        var existing = (await repo.GetAllTrackedAsync(n => n.FoodLogEntryId == entryId, ct)).ToDictionary(n => n.NutrientDefinitionId);

        foreach (var (code, p) in per100.Where(x => !ColumnCodes.Contains(x.Key) && codeToId.ContainsKey(x.Key)))
        {
            var amount = Math.Round(p.Amount * k, 4);
            if (existing.Remove(codeToId[code], out var row))
            {
                row.Amount = amount;
                row.AmountStatus = p.Status;
            }
            else
            {
                await repo.AddAsync(new FoodLogEntryNutrient
                {
                    Id = Guid.NewGuid(), FoodLogEntryId = entryId, NutrientDefinitionId = codeToId[code],
                    Amount = amount, AmountStatus = p.Status,
                }, ct);
            }
        }
        if (existing.Count > 0) await repo.RemoveRangeAsync(existing.Values, ct);
    }

    /// <summary>Copy snapshot nguyên văn sang entry mới (copy bữa/ngày không đọc lại FoodItem). Key = entry nguồn, value = entry đích.</summary>
    public static async Task CopyAsync(IUnitOfWork uow, IReadOnlyDictionary<Guid, Guid> entryMap, CancellationToken ct)
    {
        var sourceIds = entryMap.Keys.ToList();
        var repo = uow.Repository<FoodLogEntryNutrient>();
        var rows = await repo.Query().Where(n => sourceIds.Contains(n.FoodLogEntryId)).ToListAsync(ct);
        foreach (var r in rows)
            await repo.AddAsync(new FoodLogEntryNutrient
            {
                Id = Guid.NewGuid(), FoodLogEntryId = entryMap[r.FoodLogEntryId], NutrientDefinitionId = r.NutrientDefinitionId,
                Amount = r.Amount, AmountStatus = r.AmountStatus,
            }, ct);
    }
}
