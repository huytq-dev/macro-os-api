namespace Application;

/// <summary>
/// Hồ sơ chất của món và snapshot chất của entry (ADR-008). Nguồn duy nhất: FoodNutrient (/100 g) → FoodLogEntryNutrient (snapshot).
/// Riêng 4 macro nằm ở cột FoodItem, và entry giữ macro + fiber + sodium ở cột riêng (đường nhanh cho tổng ngày) nên không ghi vào FoodLogEntryNutrients.
/// </summary>
public static class NutrientSnapshot
{
    /// <summary>Chất nằm ở cột FoodLogEntry, không ghi vào FoodLogEntryNutrients.</summary>
    public static readonly string[] ColumnCodes = ["energy_kcal", "protein_g", "carbs_g", "fat_g", "fiber_g", "sodium_mg"];

    /// <summary>Chất nằm ở cột FoodItem, không ghi vào FoodNutrients.</summary>
    public static readonly string[] FoodColumnCodes = ["energy_kcal", "protein_g", "carbs_g", "fat_g"];

    /// <summary>Mã chất → Id, một truy vấn; truyền xuống các hàm ghi để cả lô entry dùng chung.</summary>
    public static async Task<Dictionary<string, Guid>> CodeIdsAsync(IUnitOfWork uow, CancellationToken ct)
        => (await CodesAsync(uow, ct)).ByCode;

    /// <summary>Id → mã chất (cùng nguồn cache với <see cref="CodeIdsAsync"/>); chỉ đọc.</summary>
    public static async Task<Dictionary<Guid, string>> CodeByIdAsync(IUnitOfWork uow, CancellationToken ct)
        => (await CodesAsync(uow, ct)).ById;

    // Danh mục chất đổi rất hiếm (chỉ khi seed lúc khởi động): cache 10 phút, bớt một vòng đi về mỗi lần log
    private static async Task<(Dictionary<string, Guid> ByCode, Dictionary<Guid, string> ById)> CodesAsync(IUnitOfWork uow, CancellationToken ct)
    {
        if (uow.Cache.TryGet<(Dictionary<string, Guid>, Dictionary<Guid, string>)>("nutrient-codes", out var cached)) return cached;
        var rows = await uow.Repository<NutrientDefinition>().Query().Select(d => new { d.Id, d.Code }).ToListAsync(ct);
        var codes = (rows.ToDictionary(d => d.Code, d => d.Id), rows.ToDictionary(d => d.Id, d => d.Code));
        uow.Cache.Set("nutrient-codes", codes, TimeSpan.FromMinutes(10));
        return codes;
    }

    /// <summary>Hồ sơ /100 g của các món (macro từ cột FoodItem, còn lại từ FoodNutrient, chất tổng thiếu được suy ra bởi <see cref="NutrientDerivation"/>), một truy vấn cho cả lô. Chất chưa biết (null) vắng mặt.</summary>
    public static async Task<Dictionary<Guid, Dictionary<string, NutrientValue>>> LoadProfilesAsync(
        IUnitOfWork uow, IReadOnlyCollection<FoodItem> foods, CancellationToken ct)
    {
        var ids = foods.Select(f => f.Id).ToList();
        var rows = await uow.Repository<FoodNutrient>().Query()
            .Where(n => ids.Contains(n.FoodItemId) && n.AmountPer100g != null)
            .Select(n => new { n.FoodItemId, n.Nutrient.Code, Amount = n.AmountPer100g!.Value, n.AmountStatus })
            .ToListAsync(ct);

        var result = foods.ToDictionary(f => f.Id, f => new Dictionary<string, NutrientValue>
        {
            ["energy_kcal"] = new(f.CaloriesPer100g, NutrientAmountStatus.Measured),
            ["protein_g"] = new(f.ProteinPer100g, NutrientAmountStatus.Measured),
            ["carbs_g"] = new(f.CarbsPer100g, NutrientAmountStatus.Measured),
            ["fat_g"] = new(f.FatPer100g, NutrientAmountStatus.Measured),
        });
        foreach (var r in rows)
            if (!FoodColumnCodes.Contains(r.Code)) result[r.FoodItemId][r.Code] = new NutrientValue(r.Amount, r.AmountStatus);
        // Chất tổng còn thiếu (omega-3/6, vitamin D từ IU, vitamin A RAE) suy từ các phần đã biết; snapshot khi log, chi tiết món và recipe cùng dùng hồ sơ này
        foreach (var profile in result.Values) NutrientDerivation.Apply(profile);
        return result;
    }

    public static async Task<Dictionary<string, NutrientValue>> LoadProfileAsync(IUnitOfWork uow, FoodItem food, CancellationToken ct)
        => (await LoadProfilesAsync(uow, [food], ct))[food.Id];

    /// <summary>Ghi snapshot cho entry vừa tạo (chưa có dòng nào): factor = gram/100 với món, số khẩu phần với recipe (hồ sơ tính trên 1 khẩu phần).</summary>
    public static async Task AddAsync(IUnitOfWork uow, Guid entryId, IReadOnlyDictionary<string, NutrientValue> profile, decimal factor,
        IReadOnlyDictionary<string, Guid> codeIds, CancellationToken ct)
    {
        var repo = uow.Repository<FoodLogEntryNutrient>();
        foreach (var (code, v) in profile.Where(x => !ColumnCodes.Contains(x.Key) && codeIds.ContainsKey(x.Key)))
            await repo.AddAsync(new FoodLogEntryNutrient
            {
                Id = Guid.NewGuid(), FoodLogEntryId = entryId, NutrientDefinitionId = codeIds[code],
                Amount = NutritionRounding.Round(v.Amount * factor, NutritionRounding.Micro), AmountStatus = v.Status,
            }, ct);
    }

    /// <summary>Nhân snapshot đã lưu với <paramref name="ratio"/> (đổi số lượng/khẩu phần của entry: không đọc lại món/recipe, lịch sử không đổi; chất vắng vẫn vắng).</summary>
    public static async Task ScaleAsync(IUnitOfWork uow, Guid entryId, decimal ratio, CancellationToken ct)
    {
        var rows = await uow.Repository<FoodLogEntryNutrient>().GetAllTrackedAsync(n => n.FoodLogEntryId == entryId && n.Amount != null, ct);
        foreach (var r in rows) r.Amount = NutritionCalculator.Rescale(r.Amount, ratio, NutritionRounding.Micro);
    }

    /// <summary>Copy snapshot nguyên văn sang entry mới (copy bữa/ngày không đọc lại FoodItem). Key = entry nguồn, value = entry đích.</summary>
    public static Task CopyAsync(IUnitOfWork uow, IReadOnlyDictionary<Guid, Guid> entryMap, CancellationToken ct)
        => CopyAsync(uow, entryMap.Select(kv => (Source: kv.Key, Copy: kv.Value)).ToList(), ct);

    /// <summary>Copy snapshot nutrient của entry nguồn sang các bản sao; một nguồn có thể có nhiều bản sao (copy sang nhiều ngày). Một truy vấn đọc cho cả lô.</summary>
    public static async Task CopyAsync(IUnitOfWork uow, IReadOnlyCollection<(Guid Source, Guid Copy)> pairs, CancellationToken ct)
    {
        var sourceIds = pairs.Select(p => p.Source).Distinct().ToList();
        var repo = uow.Repository<FoodLogEntryNutrient>();
        var bySource = (await repo.Query().Where(n => sourceIds.Contains(n.FoodLogEntryId)).ToListAsync(ct)).ToLookup(n => n.FoodLogEntryId);
        foreach (var (source, copy) in pairs)
            foreach (var r in bySource[source])
                await repo.AddAsync(new FoodLogEntryNutrient
                {
                    Id = Guid.NewGuid(), FoodLogEntryId = copy, NutrientDefinitionId = r.NutrientDefinitionId,
                    Amount = r.Amount, AmountStatus = r.AmountStatus,
                }, ct);
    }
}
