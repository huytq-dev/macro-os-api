namespace Application;

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
