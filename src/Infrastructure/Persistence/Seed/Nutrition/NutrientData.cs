namespace Infrastructure;

/// <summary>
/// Danh mục 55 nutrient cho trang Nutrition. Idempotent theo Code: chỉ thêm dòng chưa có, không ghi đè dòng đã có.
/// Target/Max là mức tham khảo người lớn (RDA/AI, ceiling cho sodium/caffeine/đường thêm/chất béo bão hoà/trans), không phải tư vấn y tế;
/// user chỉnh riêng ở UserNutrientTarget. Floor để trống. Amino acid chưa có mức chung (phụ thuộc cân nặng) nên để trống.
/// </summary>
public static class NutrientData
{
    // Code, tên, đơn vị, nhóm, số lẻ hiển thị, target (RDA), ceiling. Thứ tự mảng = DisplayOrder (bước 10).
    private static readonly (string Code, string Name, string Unit, NutrientGroup Group, int Precision, decimal? Target, decimal? Max)[] Nutrients =
    [
        ("energy_kcal", "Calories", "kcal", NutrientGroup.Energy, 0, null, null),
        ("protein_g", "Protein", "g", NutrientGroup.Macro, 1, null, null),
        ("carbs_g", "Carbs", "g", NutrientGroup.Macro, 1, null, null),
        ("fat_g", "Fat", "g", NutrientGroup.Macro, 1, null, null),

        ("fiber_g", "Fiber", "g", NutrientGroup.Carb, 1, 30, null),
        ("starch_g", "Starch", "g", NutrientGroup.Carb, 1, null, null),
        ("sugars_g", "Sugars", "g", NutrientGroup.Carb, 1, null, null),
        ("sugars_added_g", "Sugars Added", "g", NutrientGroup.Carb, 1, null, 50),

        ("fat_monounsaturated_g", "Monounsaturated", "g", NutrientGroup.Fat, 1, null, null),
        ("fat_polyunsaturated_g", "Polyunsaturated", "g", NutrientGroup.Fat, 1, null, null),
        ("omega3_g", "Omega-3", "g", NutrientGroup.Fat, 2, null, null),
        ("omega3_ala_g", "Omega-3 ALA", "g", NutrientGroup.Fat, 2, 1.6m, null),
        ("omega3_dha_g", "Omega-3 DHA", "g", NutrientGroup.Fat, 2, null, null),
        ("omega3_epa_g", "Omega-3 EPA", "g", NutrientGroup.Fat, 2, null, null),
        ("omega6_g", "Omega-6", "g", NutrientGroup.Fat, 2, null, null),
        ("fat_saturated_g", "Saturated", "g", NutrientGroup.Fat, 1, null, 22),
        ("fat_trans_g", "Trans Fat", "g", NutrientGroup.Fat, 1, null, 2),

        ("cysteine_g", "Cysteine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("histidine_g", "Histidine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("isoleucine_g", "Isoleucine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("leucine_g", "Leucine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("lysine_g", "Lysine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("methionine_g", "Methionine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("phenylalanine_g", "Phenylalanine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("threonine_g", "Threonine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("tryptophan_g", "Tryptophan", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("tyrosine_g", "Tyrosine", "g", NutrientGroup.AminoAcid, 2, null, null),
        ("valine_g", "Valine", "g", NutrientGroup.AminoAcid, 2, null, null),

        ("vitamin_b1_mg", "B1, Thiamine", "mg", NutrientGroup.Vitamin, 2, 1.2m, null),
        ("vitamin_b2_mg", "B2, Riboflavin", "mg", NutrientGroup.Vitamin, 2, 1.3m, null),
        ("vitamin_b3_mg", "B3, Niacin", "mg", NutrientGroup.Vitamin, 1, 16, null),
        ("vitamin_b5_mg", "B5, Pantothenic Acid", "mg", NutrientGroup.Vitamin, 1, 5, null),
        ("vitamin_b6_mg", "B6, Pyridoxine", "mg", NutrientGroup.Vitamin, 2, 1.3m, null),
        ("vitamin_b12_mcg", "B12, Cobalamin", "mcg", NutrientGroup.Vitamin, 1, 2.4m, null),
        ("folate_mcg", "Folate", "mcg", NutrientGroup.Vitamin, 0, 400, null),
        ("vitamin_a_mcg", "Vitamin A", "mcg", NutrientGroup.Vitamin, 0, 900, null),
        ("vitamin_c_mg", "Vitamin C", "mg", NutrientGroup.Vitamin, 1, 90, null),
        ("vitamin_d_mcg", "Vitamin D", "mcg", NutrientGroup.Vitamin, 1, 15, null),
        ("vitamin_e_mg", "Vitamin E", "mg", NutrientGroup.Vitamin, 1, 15, null),
        ("vitamin_k_mcg", "Vitamin K", "mcg", NutrientGroup.Vitamin, 0, 120, null),

        ("calcium_mg", "Calcium", "mg", NutrientGroup.Mineral, 0, 1000, null),
        ("copper_mg", "Copper", "mg", NutrientGroup.Mineral, 2, 0.9m, null),
        ("iron_mg", "Iron", "mg", NutrientGroup.Mineral, 1, 8, null),
        ("magnesium_mg", "Magnesium", "mg", NutrientGroup.Mineral, 0, 400, null),
        ("manganese_mg", "Manganese", "mg", NutrientGroup.Mineral, 2, 2.3m, null),
        ("phosphorus_mg", "Phosphorus", "mg", NutrientGroup.Mineral, 0, 700, null),
        ("potassium_mg", "Potassium", "mg", NutrientGroup.Mineral, 0, 3400, null),
        ("selenium_mcg", "Selenium", "mcg", NutrientGroup.Mineral, 0, 55, null),
        ("sodium_mg", "Sodium", "mg", NutrientGroup.Mineral, 0, null, 2300),
        ("zinc_mg", "Zinc", "mg", NutrientGroup.Mineral, 1, 11, null),

        ("alcohol_g", "Alcohol", "g", NutrientGroup.Other, 1, null, null),
        ("caffeine_mg", "Caffeine", "mg", NutrientGroup.Other, 0, null, 400),
        ("cholesterol_mg", "Cholesterol", "mg", NutrientGroup.Other, 0, null, null),
        ("choline_mg", "Choline", "mg", NutrientGroup.Other, 0, 550, null),
        ("water_g", "Water", "g", NutrientGroup.Other, 0, 2700, null),
    ];

    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken ct = default)
    {
        var existing = await context.Set<NutrientDefinition>().Select(n => n.Code).ToListAsync(ct);

        var added = false;
        for (var i = 0; i < Nutrients.Length; i++)
        {
            var n = Nutrients[i];
            if (existing.Contains(n.Code)) continue;

            context.Set<NutrientDefinition>().Add(new NutrientDefinition
            {
                Id = Guid.NewGuid(),
                Code = n.Code,
                Name = n.Name,
                Unit = n.Unit,
                Group = n.Group,
                DisplayOrder = (i + 1) * 10,
                DisplayPrecision = (byte)n.Precision,
                ReferenceTarget = n.Target,
                ReferenceMax = n.Max,
            });
            added = true;
        }
        if (added) await context.SaveChangesAsync(ct);
    }
}
