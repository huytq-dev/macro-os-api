namespace Application;

/// <summary>Chất đã quy về NutrientDefinition (Id/Code) và lượng /100 g.</summary>
public sealed record UsdaMappedNutrient(Guid DefinitionId, string Code, decimal Amount);

public sealed record UsdaMappedFood(FoodItem Food, IReadOnlyList<UsdaMappedNutrient> Nutrients);

/// <summary>USDA → FoodItem + chất (thuần, test offline). Macro vào cột FoodItem, các chất khác thành dòng FoodNutrient.</summary>
public static class UsdaFoodMapper
{
    public sealed record Definition(Guid Id, string Code, string Unit);

    /// <summary>null nếu USDA không có năng lượng (208, hoặc Atwater 957/958) — không tạo món thiếu kcal.</summary>
    public static UsdaMappedFood? Map(UsdaFood u, IReadOnlyDictionary<string, Definition> byNumber, DateTimeOffset now)
    {
        // Số liệu đúng đơn vị của chất; µg của USDA = mcg của MacroOS. Khác đơn vị (vd 268 kJ không có định nghĩa) → bỏ
        var amounts = new Dictionary<string, decimal>();
        var mapped = new List<UsdaMappedNutrient>();
        foreach (var n in u.Nutrients)
        {
            if (!byNumber.TryGetValue(n.Number, out var def) || !SameUnit(n.Unit, def.Unit) || amounts.ContainsKey(n.Number)) continue;
            amounts[n.Number] = n.Amount;
            mapped.Add(new UsdaMappedNutrient(def.Id, def.Code, Math.Round(n.Amount, 4)));
        }

        // Atwater chỉ dùng khi thiếu 208; chúng không có định nghĩa nên đọc thẳng từ danh sách gốc
        decimal? Raw(string number) => u.Nutrients.FirstOrDefault(n => n.Number == number && n.Unit.Equals("kcal", StringComparison.OrdinalIgnoreCase)) is { } v ? v.Amount : null;
        var kcal = Raw("208") ?? Raw("957") ?? Raw("958");
        if (kcal is null) return null;

        var food = new FoodItem
        {
            Id = Guid.NewGuid(),
            Name = u.Description.Length <= 256 ? u.Description : u.Description[..256],
            Category = Category(u),
            Source = FoodSource.USDA,
            IsVerified = true,
            QualityStatus = FoodQualityStatus.Verified,
            ProviderCode = "usda",
            ExternalFoodId = u.FdcId.ToString(),
            Country = "US",
            Language = "en-US",
            SourceCapturedAt = now,
            VerifiedAt = now,
            OriginalUnits = "per 100 g",
            CaloriesPer100g = kcal.Value,
            ProteinPer100g = amounts.GetValueOrDefault("203"),
            CarbsPer100g = amounts.GetValueOrDefault("205"),
            FatPer100g = amounts.GetValueOrDefault("204"),
        };
        food.IconKey = IconKeyResolver.Resolve(food.Name, food.Category);

        // Khẩu phần theo foodPortions (món đầu là mặc định) + luôn có "100 g"
        var servings = u.Portions.Select(p => new FoodServingInput(p.Name, p.Grams, false))
            .Where(s => !s.ServingName.Equals("100 g", StringComparison.OrdinalIgnoreCase)).ToList();
        servings.Add(new FoodServingInput("100 g", 100, servings.Count == 0));
        if (servings.Count > 1) servings[0] = servings[0] with { IsDefault = true };
        FoodNutritionApplier.ReplaceServings(food, servings);
        foreach (var s in food.Servings) s.UnitType = s.WeightInGrams == 100 && s.ServingName == "100 g" ? ServingUnitType.Mass : ServingUnitType.Household;

        // Macro nằm ở cột FoodItem: không lặp lại thành FoodNutrient
        return new UsdaMappedFood(food, mapped.Where(m => !NutrientSnapshot.FoodColumnCodes.Contains(m.Code)).ToList());
    }

    private static bool SameUnit(string usda, string ours)
        => Norm(usda) == Norm(ours);

    private static string Norm(string unit)
    {
        var u = unit.Trim().ToLowerInvariant();
        return u is "µg" or "μg" or "ug" ? "mcg" : u;
    }

    private static FoodCategory Category(UsdaFood u)
    {
        var c = (u.Category ?? "").ToLowerInvariant();
        var name = u.Description.ToLowerInvariant();
        if (name.StartsWith("egg")) return FoodCategory.Protein;
        if (c.Contains("fruit")) return FoodCategory.Fruits;
        if (c.Contains("vegetable")) return FoodCategory.Vegetables;
        if (c.Contains("dairy") || c.Contains("milk") || c.Contains("cheese")) return FoodCategory.Dairy;
        if (c.Contains("beef") || c.Contains("pork") || c.Contains("poultry") || c.Contains("fish") || c.Contains("lamb") || c.Contains("sausage")
            || c.Contains("legume") || c.Contains("finfish") || c.Contains("shellfish")) return FoodCategory.Protein;
        if (c.Contains("cereal") || c.Contains("grain") || c.Contains("pasta") || c.Contains("baked") || c.Contains("bread")) return FoodCategory.Grains;
        if (c.Contains("fat") || c.Contains("oil") || c.Contains("nut") || c.Contains("seed")) return FoodCategory.Fats;
        if (c.Contains("beverage")) return FoodCategory.Beverages;
        if (c.Contains("snack") || c.Contains("sweet") || c.Contains("candy")) return FoodCategory.Snacks;
        if (c.Contains("fast food") || c.Contains("restaurant")) return FoodCategory.FastFood;
        return FoodCategory.Other;
    }
}
