namespace Application;

/// <summary>
/// Đọc/ghi chất của món ở FoodNutrient (/100 g). 4 macro nằm ở cột FoodItem; 11 trường còn lại của <see cref="FoodNutrition"/> (contract cũ)
/// và mọi chất khác (<see cref="FoodNutrientInput"/>) đều là dòng FoodNutrient như nhau.
/// </summary>
public static class FoodNutrientStore
{
    // Trường của FoodNutrition (ngoài 4 macro) ↔ NutrientDefinition.Code
    private static readonly (string Code, Func<FoodNutrition, decimal?> Get)[] Fields =
    [
        ("fiber_g", n => n.FiberPer100g), ("sugars_g", n => n.SugarPer100g), ("sodium_mg", n => n.SodiumPer100g),
        ("potassium_mg", n => n.PotassiumPer100g), ("calcium_mg", n => n.CalciumPer100g), ("iron_mg", n => n.IronPer100g),
        ("vitamin_c_mg", n => n.VitaminCPer100g), ("vitamin_d_mcg", n => n.VitaminDPer100g),
        ("fat_saturated_g", n => n.SaturatedFatPer100g), ("fat_trans_g", n => n.TransFatPer100g), ("cholesterol_mg", n => n.CholesterolPer100g),
    ];

    public static readonly IReadOnlyList<string> ContractCodes = Fields.Select(f => f.Code).ToList();

    /// <summary>Lượng cần ghi: 11 trường của <paramref name="n"/> (bỏ null), rồi <paramref name="extras"/> ghi đè/bổ sung.</summary>
    public static Dictionary<string, decimal> Amounts(FoodNutrition n, IReadOnlyList<FoodNutrientInput>? extras)
    {
        var amounts = new Dictionary<string, decimal>();
        foreach (var (code, get) in Fields)
            if (get(n) is { } v) amounts[code] = v;
        if (extras is not null)
            foreach (var e in extras) amounts[e.Code] = e.AmountPer100g;
        return amounts;
    }

    /// <summary>
    /// Ghi FoodNutrient của món. <paramref name="replaceAll"/> = true: <paramref name="amounts"/> là toàn bộ chất ngoài macro (dòng không có trong đó bị xoá);
    /// false: chỉ 11 chất của contract cũ được đồng bộ (vắng = xoá), chất khác giữ nguyên. Mã không tồn tại → 400, chưa ghi gì.
    /// </summary>
    public static async Task SaveAsync(IUnitOfWork uow, Guid foodId, IReadOnlyDictionary<string, decimal> amounts, bool replaceAll, bool isNew,
        IReadOnlyDictionary<string, Guid> codeIds, CancellationToken ct)
    {
        foreach (var code in amounts.Keys)
            if (!codeIds.ContainsKey(code) || NutrientSnapshot.FoodColumnCodes.Contains(code))
                throw new BadRequestException($"Mã chất không hợp lệ: {code}.");

        var repo = uow.Repository<FoodNutrient>();
        var existing = isNew
            ? new Dictionary<Guid, FoodNutrient>()
            : (await repo.GetAllTrackedAsync(n => n.FoodItemId == foodId, ct)).ToDictionary(n => n.NutrientDefinitionId);

        foreach (var (code, amount) in amounts)
        {
            var id = codeIds[code];
            if (existing.Remove(id, out var row))
            {
                row.AmountPer100g = amount;
                row.AmountStatus = NutrientAmountStatus.Measured;
            }
            else
            {
                await repo.AddAsync(new FoodNutrient
                {
                    Id = Guid.NewGuid(), FoodItemId = foodId, NutrientDefinitionId = id,
                    AmountPer100g = amount, AmountStatus = NutrientAmountStatus.Measured,
                }, ct);
            }
        }

        // Dòng còn lại trong `existing` không được gửi lên: xoá theo phạm vi sở hữu của lần ghi này
        var owned = replaceAll ? existing.Values : existing.Values.Where(n => ContractCodes.Any(c => codeIds[c] == n.NutrientDefinitionId));
        var stale = owned.ToList();
        if (stale.Count > 0) await repo.RemoveRangeAsync(stale, ct);
    }

    /// <summary>FoodNutrition (4 macro + 11 chất của contract cũ) từ cột FoodItem và hồ sơ chất.</summary>
    public static FoodNutrition ToContract(FoodItem f, IReadOnlyDictionary<string, NutrientValue> profile)
    {
        decimal? Get(string code) => profile.TryGetValue(code, out var v) ? v.Amount : null;
        return new FoodNutrition(
            f.CaloriesPer100g, f.ProteinPer100g, f.CarbsPer100g, f.FatPer100g,
            Get("fiber_g"), Get("sugars_g"), Get("sodium_mg"), Get("potassium_mg"), Get("calcium_mg"), Get("iron_mg"),
            Get("vitamin_c_mg"), Get("vitamin_d_mcg"), Get("fat_saturated_g"), Get("fat_trans_g"), Get("cholesterol_mg"));
    }

    /// <summary>Response chi tiết món: macro + mọi chất ngoài macro (đọc FoodNutrient một truy vấn).</summary>
    public static async Task<FoodItemResponse> ToResponseAsync(IUnitOfWork uow, FoodItem food, bool isMine, Guid userId, CancellationToken ct)
    {
        var profile = await NutrientSnapshot.LoadProfileAsync(uow, food, ct);
        var overrides = await FoodIconSupport.OverridesAsync(uow, userId, [food.Id], ct);
        return food.Adapt<FoodItemResponse>() with
        {
            IsMine = isMine,
            Nutrition = ToContract(food, profile),
            Nutrients = profile.Where(p => !NutrientSnapshot.FoodColumnCodes.Contains(p.Key))
                .OrderBy(p => p.Key).Select(p => new FoodNutrientValue(p.Key, p.Value.Amount)).ToList(),
            NutrientsPer100g = ByFeKey(profile),
            Emoji = FoodEmoji.For(food.Category),
            IconKey = FoodIconSupport.Effective(food.Id, food.IconKey, overrides),
            GramsPerMl = FoodServingText.GramsPerMl(food),
        };
    }

    /// <summary>
    /// Mọi chất trên 100 g theo khoá camelCase của FE (đủ 56 khoá của <see cref="NutritionOverviewKeys"/>); chất chưa có dữ liệu = null, khác 0.
    /// <c>netCarbs</c> = carbs − fiber (không âm).
    /// </summary>
    public static Dictionary<string, decimal?> ByFeKey(IReadOnlyDictionary<string, NutrientValue> profile)
    {
        var result = NutritionOverviewKeys.ByCode.ToDictionary(
            k => k.Value, k => profile.TryGetValue(k.Key, out var v) ? (decimal?)v.Amount : null);
        result[NutritionOverviewKeys.NetCarbs] = profile.TryGetValue("carbs_g", out var carbs)
            ? NutritionCalculator.NetCarbs(carbs.Amount, profile.TryGetValue("fiber_g", out var fiber) ? fiber.Amount : null)
            : null;
        return result;
    }
}
