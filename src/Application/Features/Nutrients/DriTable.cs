namespace Application;

/// <param name="Floor">Mức tối thiểu thật sự (LTI) — chỉ đặt khi có số liệu thật; hiện chưa chất nào có nên luôn null (RDA/AI là Target, không phải Floor).</param>
/// <param name="Target">Mục tiêu (RDA/AI); null nếu chất chỉ có trần.</param>
/// <param name="Ceiling">Trần (UL chỉ khi UL áp cho tổng lượng ăn từ thực phẩm; trần khuyến nghị DGA/FDA/CDRR); null nếu không có trần.</param>
public readonly record struct DriRange(decimal? Floor, decimal? Target, decimal? Ceiling)
{
    public static readonly DriRange None = new(null, null, null);
    public bool IsNone => Floor is null && Target is null && Ceiling is null;
}

/// <summary>
/// Mức khuyến nghị theo giới + nhóm tuổi (DRI của NASEM/IOM, người lớn không mang thai/cho con bú), thuần, không DB. Tra theo (mã chất, giới, tuổi, kcal mục tiêu/ngày).
/// Thiếu hồ sơ (không có ngày sinh, dưới 19 tuổi, giới "Other") → "mức tham chiếu": Target/Floor lấy mức CAO NHẤT trong các nhóm có thể áp dụng (không báo thiếu sai), Ceiling lấy mức THẤP NHẤT.
/// Ceiling chỉ đặt khi UL áp cho tổng lượng ăn (kể cả thực phẩm); UL chỉ cho dạng bổ sung/tổng hợp (Mg, niacin, vitamin E, folate DFE) và UL của retinol (không áp cho vitamin A tổng) bị bỏ.
/// Chất có trong bảng nhưng DRI không đặt mức nào (EPA, DHA, amino acid, trans…) trả <see cref="DriRange.None"/>: DRI không định nghĩa trường nào nên mức seed (vd trần trans 2 g) được GIỮ
/// (xem <see cref="NutrientGoals.Resolve"/>: seed → DRI chỉ ở trường DRI có định nghĩa → user); chất không có trong bảng trả null (dùng seed).
/// Nguồn đã đối chiếu (NIH ODS fact sheet): vitamin A/C/D/E/K/B6/B12/folate/choline, Ca, Cu, Fe, Mg, Mn, P, K, Se, Zn, ALA. Nguồn Master xác nhận: nước (NASEM 2004), Mg UL chỉ dạng bổ sung, đường thêm &lt; 10% kcal (DGA).
/// Số còn lại CHƯA đối chiếu văn bản gốc (đánh dấu "nguồn chưa đối chiếu" cạnh từng số): xem comment trong <see cref="Entry"/>.
/// </summary>
public static class DriTable
{
    /// <summary>Tuổi tối thiểu có bảng DRI người lớn (nhóm 19–30).</summary>
    public const int MinAdultAge = 19;

    /// <summary>Tuổi (năm tròn) tại <paramref name="on"/>; null nếu chưa có ngày sinh.</summary>
    public static int? AgeOn(DateOnly? dateOfBirth, DateOnly on)
        => dateOfBirth is { } dob ? on.Year - dob.Year - (on < dob.AddYears(on.Year - dob.Year) ? 1 : 0) : null;

    /// <summary>Nhóm tuổi: 0 = 19–30, 1 = 31–50, 2 = 51–70, 3 = 71+.</summary>
    public static int Bucket(int age) => age <= 30 ? 0 : age <= 50 ? 1 : age <= 70 ? 2 : 3;

    /// <summary>
    /// null = chất không thuộc bảng DRI (caller dùng mức seed); khác null = mức DRI đã tính.
    /// <paramref name="isReference"/> = true khi thiếu giới/tuổi nên phải lấy mức tham chiếu người lớn (nam/nữ, mọi nhóm tuổi).
    /// </summary>
    public static DriRange? Resolve(string code, Gender gender, int? age, decimal? kcalTarget, out bool isReference)
    {
        var known = age is >= MinAdultAge && gender is Gender.Male or Gender.Female;
        isReference = !known;
        bool[] sexes = known ? [gender == Gender.Male] : [true, false];
        int[] buckets = known ? [Bucket(age!.Value)] : [0, 1, 2, 3];

        decimal? floor = null, target = null, ceiling = null;
        var any = false;
        foreach (var male in sexes)
            foreach (var bucket in buckets)
            {
                if (Entry(code, male, bucket, kcalTarget) is not { } e) return null;
                any = true;
                floor = Max(floor, e.Floor);
                target = Max(target, e.Target);
                ceiling = Min(ceiling, e.Ceiling);
            }
        return any ? new DriRange(floor, target, ceiling) : null;
    }

    private static decimal? Max(decimal? a, decimal? b) => a is null ? b : b is null ? a : Math.Max(a.Value, b.Value);
    private static decimal? Min(decimal? a, decimal? b) => a is null ? b : b is null ? a : Math.Min(a.Value, b.Value);

    /// <summary>RDA/AI = Target, UL (nếu áp cho tổng lượng ăn) = Ceiling. Floor KHÔNG đặt: chưa có số LTI thật nên không bịa (floor = target chỉ là dấu giả).</summary>
    private static DriRange Rda(decimal v, decimal? ul = null) => new(null, v, ul);

    /// <summary>Mức của một (giới, nhóm tuổi); null = chất không thuộc bảng (hoặc cần kcal mà chưa có).</summary>
    private static DriRange? Entry(string code, bool male, int b, decimal? kcal)
    {
        DriRange? PerKcal(Func<decimal, DriRange> f) => kcal is > 0 ? f(kcal.Value) : null;
        return code switch
        {
            // --- Vitamin (NIH ODS đã đối chiếu). UL của A (chỉ retinol), E, niacin (chỉ dạng bổ sung), folate (chỉ folic acid) KHÔNG đặt làm trần ---
            "vitamin_a_mcg" => Rda(male ? 900 : 700),
            "vitamin_c_mg" => Rda(male ? 90 : 75, 2000),
            "vitamin_d_mcg" => Rda(b == 3 ? 20 : 15, 100),
            "vitamin_e_mg" => Rda(15),
            "vitamin_k_mcg" => Rda(male ? 120 : 90),
            "vitamin_b1_mg" => Rda(male ? 1.2m : 1.1m),
            "vitamin_b2_mg" => Rda(male ? 1.3m : 1.1m),
            "vitamin_b3_mg" => Rda(male ? 16 : 14),
            "vitamin_b5_mg" => Rda(5),
            "vitamin_b6_mg" => Rda(b >= 2 ? (male ? 1.7m : 1.5m) : 1.3m, 100),
            "vitamin_b12_mcg" => Rda(2.4m),
            "folate_mcg" => Rda(400),
            "choline_mg" => Rda(male ? 550 : 425, 3500),

            // --- Khoáng (NIH ODS đã đối chiếu); Mg UL 350 chỉ dạng bổ sung nên không đặt trần ---
            "calcium_mg" => Rda(male ? (b == 3 ? 1200 : 1000) : (b >= 2 ? 1200 : 1000), b >= 2 ? 2000 : 2500),
            "copper_mg" => Rda(0.9m, 10),
            "iron_mg" => Rda(male ? 8 : (b < 2 ? 18 : 8), 45),
            "magnesium_mg" => Rda(male ? (b == 0 ? 400 : 420) : (b == 0 ? 310 : 320)),
            "manganese_mg" => Rda(male ? 2.3m : 1.8m, 11),
            "phosphorus_mg" => Rda(700, b == 3 ? 3000 : 4000),
            "potassium_mg" => Rda(male ? 3400 : 2600),
            "selenium_mcg" => Rda(55, 400),
            "zinc_mg" => Rda(male ? 11 : 8, 40),
            // Na: AI 1500 + CDRR 2300 (DRI 2019) — nguồn chưa đối chiếu văn bản gốc (NASEM 2019)
            "sodium_mg" => new DriRange(null, 1500, 2300),

            // --- Chất béo / carb / khác ---
            "omega3_ala_g" => Rda(male ? 1.6m : 1.1m), // AI (NIH ODS đã đối chiếu)
            // Omega-6 = AI của linoleic (17/12 g tuổi 19–50, 14/11 g từ 51), omega6_g tổng chỉ gần đúng — nguồn chưa đối chiếu văn bản gốc (NASEM 2005)
            "omega6_g" => Rda(b < 2 ? (male ? 17 : 12) : (male ? 14 : 11)),
            // Nước tổng (kể cả nước từ thức ăn) AI 3.7 L nam / 2.7 L nữ: NASEM 2004 (Master đã xác nhận)
            "water_g" => Rda(male ? 3700 : 2700),
            // Fiber AI 14 g / 1000 kcal — nguồn chưa đối chiếu văn bản gốc (IOM 2005)
            "fiber_g" => PerKcal(k => Rda(Math.Round(14m * k / 1000m, 1))),
            // Đường thêm < 10% năng lượng (DGA 2020–2025, Master đã xác nhận); không có số USDA nên chỉ có khi user nhập/nhãn
            "sugars_added_g" => PerKcal(k => new DriRange(null, null, Math.Round(0.10m * k / 4m, 1))),
            // Béo bão hoà < 10% năng lượng — nguồn chưa đối chiếu văn bản gốc (DGA 2020–2025)
            "fat_saturated_g" => PerKcal(k => new DriRange(null, null, Math.Round(0.10m * k / 9m, 1))),
            // Caffeine 400 mg/ngày người lớn khoẻ mạnh — nguồn chưa đối chiếu văn bản gốc (FDA); không phải DRI
            "caffeine_mg" => new DriRange(null, null, 400),

            // Không có DRI: DRI không đặt mức nào (trans "càng thấp càng tốt", cholesterol DGA 2015+ bỏ mốc 300 mg); seed nếu có vẫn giữ
            "fat_trans_g" or "cholesterol_mg" or "starch_g" or "sugars_g" or "alcohol_g"
                or "fat_monounsaturated_g" or "fat_polyunsaturated_g" or "omega3_g" or "omega3_epa_g" or "omega3_dha_g"
                or "cysteine_g" or "histidine_g" or "isoleucine_g" or "leucine_g" or "lysine_g" or "methionine_g"
                or "phenylalanine_g" or "threonine_g" or "tryptophan_g" or "tyrosine_g" or "valine_g" => DriRange.None,
            _ => null,
        };
    }
}
