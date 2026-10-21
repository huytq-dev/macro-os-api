namespace Infrastructure;

/// <summary>
/// Danh mục nutrient: 55 chất core cho trang Nutrition + 85 chất mở rộng của USDA (IsCore = false). Idempotent theo Code: chỉ thêm dòng chưa có và bổ sung UsdaNumber cho dòng đã có, không ghi đè phần còn lại.
/// Target/Max là mức tham khảo người lớn (RDA/AI, ceiling cho sodium/caffeine/đường thêm/chất béo bão hoà; béo trans < 1% kcal tính ở DriTable, không seed), không phải tư vấn y tế;
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
        ("fat_trans_g", "Trans Fat", "g", NutrientGroup.Fat, 1, null, null),

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

    // Số chất USDA FoodData Central của 52 chất core (omega3_g, omega6_g, sugars_added_g là chất tính toán/nhập tay: không có số USDA)
    private static readonly Dictionary<string, string> CoreUsdaNumbers = new()
    {
        ["protein_g"] = "203",
        ["fat_g"] = "204",
        ["carbs_g"] = "205",
        ["energy_kcal"] = "208",
        ["starch_g"] = "209",
        ["alcohol_g"] = "221",
        ["water_g"] = "255",
        ["caffeine_mg"] = "262",
        ["sugars_g"] = "269",
        ["fiber_g"] = "291",
        ["calcium_mg"] = "301",
        ["iron_mg"] = "303",
        ["magnesium_mg"] = "304",
        ["phosphorus_mg"] = "305",
        ["potassium_mg"] = "306",
        ["sodium_mg"] = "307",
        ["zinc_mg"] = "309",
        ["copper_mg"] = "312",
        ["manganese_mg"] = "315",
        ["selenium_mcg"] = "317",
        ["vitamin_a_mcg"] = "320",
        ["vitamin_e_mg"] = "323",
        ["vitamin_d_mcg"] = "328",
        ["vitamin_c_mg"] = "401",
        ["vitamin_b1_mg"] = "404",
        ["vitamin_b2_mg"] = "405",
        ["vitamin_b3_mg"] = "406",
        ["vitamin_b5_mg"] = "410",
        ["vitamin_b6_mg"] = "415",
        ["folate_mcg"] = "417",
        ["vitamin_b12_mcg"] = "418",
        ["choline_mg"] = "421",
        ["vitamin_k_mcg"] = "430",
        ["tryptophan_g"] = "501",
        ["threonine_g"] = "502",
        ["isoleucine_g"] = "503",
        ["leucine_g"] = "504",
        ["lysine_g"] = "505",
        ["methionine_g"] = "506",
        ["cysteine_g"] = "507",
        ["phenylalanine_g"] = "508",
        ["tyrosine_g"] = "509",
        ["valine_g"] = "510",
        ["histidine_g"] = "512",
        ["cholesterol_mg"] = "601",
        ["fat_trans_g"] = "605",
        ["fat_saturated_g"] = "606",
        ["omega3_dha_g"] = "621",
        ["omega3_epa_g"] = "629",
        ["fat_monounsaturated_g"] = "645",
        ["fat_polyunsaturated_g"] = "646",
        ["omega3_ala_g"] = "851",
    };

    // 86 chất mở rộng của USDA (IsCore = false): số USDA, code, tên, đơn vị (µg ghi mcg), nhóm, số lẻ. DisplayOrder nối tiếp các chất core.
    private static readonly (string Usda, string Code, string Name, string Unit, NutrientGroup Group, int Precision)[] ExtendedNutrients =
    [
        ("207", "ash_g", "Ash", "g", NutrientGroup.Carb, 2),
        ("210", "sucrose_g", "Sucrose", "g", NutrientGroup.Carb, 2),
        ("211", "glucose_g", "Glucose", "g", NutrientGroup.Carb, 2),
        ("212", "fructose_g", "Fructose", "g", NutrientGroup.Carb, 2),
        ("213", "lactose_g", "Lactose", "g", NutrientGroup.Carb, 2),
        ("214", "maltose_g", "Maltose", "g", NutrientGroup.Carb, 2),
        ("263", "theobromine_mg", "Theobromine", "mg", NutrientGroup.Other, 2),
        ("287", "galactose_g", "Galactose", "g", NutrientGroup.Carb, 2),
        ("313", "fluoride_mcg", "Fluoride", "mcg", NutrientGroup.Mineral, 1),
        ("318", "vitamin_a_iu", "Vitamin A, IU", "IU", NutrientGroup.Vitamin, 0),
        ("319", "retinol_mcg", "Retinol", "mcg", NutrientGroup.Vitamin, 1),
        ("321", "carotene_beta_mcg", "Carotene, Beta", "mcg", NutrientGroup.Vitamin, 1),
        ("322", "carotene_alpha_mcg", "Carotene, Alpha", "mcg", NutrientGroup.Vitamin, 1),
        ("324", "vitamin_d_iu", "Vitamin D, IU", "IU", NutrientGroup.Vitamin, 0),
        ("326", "vitamin_d3_mcg", "Vitamin D3, Cholecalciferol", "mcg", NutrientGroup.Vitamin, 1),
        ("334", "cryptoxanthin_beta_mcg", "Cryptoxanthin, Beta", "mcg", NutrientGroup.Vitamin, 1),
        ("337", "lycopene_mcg", "Lycopene", "mcg", NutrientGroup.Vitamin, 1),
        ("338", "lutein_zeaxanthin_mcg", "Lutein + Zeaxanthin", "mcg", NutrientGroup.Vitamin, 1),
        ("341", "tocopherol_beta_mg", "Tocopherol, Beta", "mg", NutrientGroup.Vitamin, 2),
        ("342", "tocopherol_gamma_mg", "Tocopherol, Gamma", "mg", NutrientGroup.Vitamin, 2),
        ("343", "tocopherol_delta_mg", "Tocopherol, Delta", "mg", NutrientGroup.Vitamin, 2),
        ("344", "tocotrienol_alpha_mg", "Tocotrienol, Alpha", "mg", NutrientGroup.Vitamin, 2),
        ("345", "tocotrienol_beta_mg", "Tocotrienol, Beta", "mg", NutrientGroup.Vitamin, 2),
        ("346", "tocotrienol_gamma_mg", "Tocotrienol, Gamma", "mg", NutrientGroup.Vitamin, 2),
        ("347", "tocotrienol_delta_mg", "Tocotrienol, Delta", "mg", NutrientGroup.Vitamin, 2),
        ("428", "vitamin_k_mk4_mcg", "Vitamin K, Menaquinone-4", "mcg", NutrientGroup.Vitamin, 1),
        ("429", "vitamin_k_dihydro_mcg", "Vitamin K, Dihydrophylloquinone", "mcg", NutrientGroup.Vitamin, 1),
        ("431", "folic_acid_mcg", "Folic Acid", "mcg", NutrientGroup.Vitamin, 1),
        ("432", "folate_food_mcg", "Folate, Food", "mcg", NutrientGroup.Vitamin, 1),
        ("435", "folate_dfe_mcg", "Folate, DFE", "mcg", NutrientGroup.Vitamin, 1),
        ("454", "betaine_mg", "Betaine", "mg", NutrientGroup.Other, 2),
        ("511", "arginine_g", "Arginine", "g", NutrientGroup.AminoAcid, 2),
        ("513", "alanine_g", "Alanine", "g", NutrientGroup.AminoAcid, 2),
        ("514", "aspartic_acid_g", "Aspartic Acid", "g", NutrientGroup.AminoAcid, 2),
        ("515", "glutamic_acid_g", "Glutamic Acid", "g", NutrientGroup.AminoAcid, 2),
        ("516", "glycine_g", "Glycine", "g", NutrientGroup.AminoAcid, 2),
        ("517", "proline_g", "Proline", "g", NutrientGroup.AminoAcid, 2),
        ("518", "serine_g", "Serine", "g", NutrientGroup.AminoAcid, 2),
        ("521", "hydroxyproline_g", "Hydroxyproline", "g", NutrientGroup.AminoAcid, 2),
        ("573", "vitamin_e_added_mg", "Vitamin E, Added", "mg", NutrientGroup.Vitamin, 2),
        ("578", "vitamin_b12_added_mcg", "B12, Added", "mcg", NutrientGroup.Vitamin, 1),
        ("607", "sfa_4_0_g", "SFA 4:0", "g", NutrientGroup.Fat, 2),
        ("608", "sfa_6_0_g", "SFA 6:0", "g", NutrientGroup.Fat, 2),
        ("609", "sfa_8_0_g", "SFA 8:0", "g", NutrientGroup.Fat, 2),
        ("610", "sfa_10_0_g", "SFA 10:0", "g", NutrientGroup.Fat, 2),
        ("611", "sfa_12_0_g", "SFA 12:0", "g", NutrientGroup.Fat, 2),
        ("612", "sfa_14_0_g", "SFA 14:0", "g", NutrientGroup.Fat, 2),
        ("613", "sfa_16_0_g", "SFA 16:0", "g", NutrientGroup.Fat, 2),
        ("614", "sfa_18_0_g", "SFA 18:0", "g", NutrientGroup.Fat, 2),
        ("615", "sfa_20_0_g", "SFA 20:0", "g", NutrientGroup.Fat, 2),
        ("617", "mufa_18_1_g", "MUFA 18:1", "g", NutrientGroup.Fat, 2),
        ("618", "pufa_18_2_g", "PUFA 18:2", "g", NutrientGroup.Fat, 2),
        ("619", "pufa_18_3_g", "PUFA 18:3", "g", NutrientGroup.Fat, 2),
        ("620", "pufa_20_4_g", "PUFA 20:4", "g", NutrientGroup.Fat, 2),
        ("624", "sfa_22_0_g", "SFA 22:0", "g", NutrientGroup.Fat, 2),
        ("625", "mufa_14_1_g", "MUFA 14:1", "g", NutrientGroup.Fat, 2),
        ("626", "mufa_16_1_g", "MUFA 16:1", "g", NutrientGroup.Fat, 2),
        ("627", "pufa_18_4_g", "PUFA 18:4", "g", NutrientGroup.Fat, 2),
        ("628", "mufa_20_1_g", "MUFA 20:1", "g", NutrientGroup.Fat, 2),
        ("630", "mufa_22_1_g", "MUFA 22:1", "g", NutrientGroup.Fat, 2),
        ("631", "pufa_22_5_n3_g", "PUFA 22:5 n-3 (DPA)", "g", NutrientGroup.Fat, 2),
        ("636", "phytosterols_mg", "Phytosterols", "mg", NutrientGroup.Fat, 2),
        ("652", "sfa_15_0_g", "SFA 15:0", "g", NutrientGroup.Fat, 2),
        ("653", "sfa_17_0_g", "SFA 17:0", "g", NutrientGroup.Fat, 2),
        ("654", "sfa_24_0_g", "SFA 24:0", "g", NutrientGroup.Fat, 2),
        ("662", "tfa_16_1_t_g", "TFA 16:1 t", "g", NutrientGroup.Fat, 2),
        ("663", "tfa_18_1_t_g", "TFA 18:1 t", "g", NutrientGroup.Fat, 2),
        ("664", "tfa_22_1_t_g", "TFA 22:1 t", "g", NutrientGroup.Fat, 2),
        ("665", "tfa_18_2_t_g", "TFA 18:2 t", "g", NutrientGroup.Fat, 2),
        ("670", "pufa_18_2_cla_g", "PUFA 18:2 CLAs", "g", NutrientGroup.Fat, 2),
        ("671", "mufa_24_1_c_g", "MUFA 24:1 c", "g", NutrientGroup.Fat, 2),
        ("672", "pufa_20_2_n6_cc_g", "PUFA 20:2 n-6 c,c", "g", NutrientGroup.Fat, 2),
        ("673", "mufa_16_1_c_g", "MUFA 16:1 c", "g", NutrientGroup.Fat, 2),
        ("674", "mufa_18_1_c_g", "MUFA 18:1 c", "g", NutrientGroup.Fat, 2),
        ("675", "pufa_18_2_n6_cc_g", "PUFA 18:2 n-6 c,c", "g", NutrientGroup.Fat, 2),
        ("676", "mufa_22_1_c_g", "MUFA 22:1 c", "g", NutrientGroup.Fat, 2),
        ("685", "pufa_18_3_n6_ccc_g", "PUFA 18:3 n-6 c,c,c", "g", NutrientGroup.Fat, 2),
        ("687", "mufa_17_1_g", "MUFA 17:1", "g", NutrientGroup.Fat, 2),
        ("689", "pufa_20_3_g", "PUFA 20:3", "g", NutrientGroup.Fat, 2),
        ("693", "fat_trans_monoenoic_g", "Trans-monoenoic", "g", NutrientGroup.Fat, 2),
        ("695", "fat_trans_polyenoic_g", "Trans-polyenoic", "g", NutrientGroup.Fat, 2),
        ("697", "mufa_15_1_g", "MUFA 15:1", "g", NutrientGroup.Fat, 2),
        ("852", "pufa_20_3_n3_g", "PUFA 20:3 n-3", "g", NutrientGroup.Fat, 2),
        ("853", "pufa_20_3_n6_g", "PUFA 20:3 n-6", "g", NutrientGroup.Fat, 2),
        ("855", "pufa_20_4_n6_g", "PUFA 20:4 n-6 (AA)", "g", NutrientGroup.Fat, 2),
        ("858", "pufa_22_4_g", "PUFA 22:4", "g", NutrientGroup.Fat, 2),
    ];

    public static async Task SeedAsync(ApplicationDbContext context, CancellationToken ct = default)
    {
        // Tracked: dòng đã có chỉ được bổ sung UsdaNumber (không ghi đè tên/target/IsCore)
        var byCode = (await context.Set<NutrientDefinition>().ToListAsync(ct)).ToDictionary(n => n.Code);

        var changed = false;
        void Ensure(int index, string code, string name, string unit, NutrientGroup group, int precision, decimal? target, decimal? max, string? usda, bool isCore)
        {
            if (byCode.TryGetValue(code, out var row))
            {
                if (row.UsdaNumber is null && usda is not null) { row.UsdaNumber = usda; changed = true; }
                return;
            }
            var def = new NutrientDefinition
            {
                Id = Guid.NewGuid(),
                Code = code,
                Name = name,
                Unit = unit,
                Group = group,
                DisplayOrder = (index + 1) * 10,
                DisplayPrecision = (byte)precision,
                ReferenceTarget = target,
                ReferenceMax = max,
                UsdaNumber = usda,
                IsCore = isCore,
            };
            context.Set<NutrientDefinition>().Add(def);
            byCode[code] = def;
            changed = true;
        }

        for (var i = 0; i < Nutrients.Length; i++)
        {
            var n = Nutrients[i];
            Ensure(i, n.Code, n.Name, n.Unit, n.Group, n.Precision, n.Target, n.Max, CoreUsdaNumbers.GetValueOrDefault(n.Code), isCore: true);
        }
        for (var j = 0; j < ExtendedNutrients.Length; j++)
        {
            var n = ExtendedNutrients[j];
            Ensure(Nutrients.Length + j, n.Code, n.Name, n.Unit, n.Group, n.Precision, null, null, n.Usda, isCore: false);
        }
        if (changed) await context.SaveChangesAsync(ct);
    }
}
