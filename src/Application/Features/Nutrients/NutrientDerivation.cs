namespace Application;

/// <summary>
/// Chất dẫn xuất khi hồ sơ chất của món THIẾU chất tổng: ALA/LA/AA từ dạng chưa phân loại của SR/FNDDS, omega-3 tổng, omega-6 tổng, vitamin A (RAE) từ retinol + carotenoid.
/// Giá trị suy ra luôn có trạng thái <see cref="NutrientAmountStatus.Estimated"/> (tổng các phần đã biết, có thể thiếu phần chưa biết); không bao giờ ghi đè chất đã có, trừ folate (xem bên dưới).
/// Chạy khi nạp hồ sơ món nên snapshot khi log, chi tiết món và recipe dùng chung.
/// Số liệu FDC (đếm trên 3 file thật): SR Legacy/FNDDS gần như chỉ có 619 (18:3), 618 (18:2), 620 (20:4) chưa phân loại; Foundation chỉ có folate 417; mọi món có vitamin D đều có 328 (µg) nên không cần quy đổi IU.
/// </summary>
public static class NutrientDerivation
{
    // 852 = 20:3 n-3; 631 = DPA; ALA lấy từ omega3_ala_g (851), thiếu thì từ pufa_18_3_g (619, gồm cả chút GLA 18:3 n-6 nên hơi cao: chấp nhận, đã đánh dấu Estimated)
    private static readonly string[] Omega3Parts = ["omega3_ala_g", "omega3_epa_g", "omega3_dha_g", "pufa_22_5_n3_g", "pufa_20_3_n3_g"];

    // LA = pufa_18_2_n6_cc_g (675) hoặc pufa_18_2_g (618); AA = pufa_20_4_n6_g (855) hoặc pufa_20_4_g (620, có thể lẫn 20:4 n-3: chấp nhận)
    private static readonly string[] Omega6Parts = ["pufa_18_3_n6_ccc_g", "pufa_20_2_n6_cc_g", "pufa_20_3_n6_g"];

    public static void Apply(Dictionary<string, NutrientValue> profile)
    {
        // Folate hiển thị là DFE (mức khuyến nghị 400 µg tính theo DFE): có 435 thì dùng nó thay cho folate tổng 417 (khác nhau ở món bổ sung folic acid); Foundation chỉ có 417 thì giữ 417
        if (profile.TryGetValue("folate_dfe_mcg", out var dfe)) profile["folate_mcg"] = dfe;

        Fallback(profile, "omega3_ala_g", "pufa_18_3_g");
        Sum(profile, "omega3_g", Omega3Parts);

        var linoleic = profile.ContainsKey("pufa_18_2_n6_cc_g") ? "pufa_18_2_n6_cc_g" : "pufa_18_2_g";
        var arachidonic = profile.ContainsKey("pufa_20_4_n6_g") ? "pufa_20_4_n6_g" : "pufa_20_4_g";
        Sum(profile, "omega6_g", [linoleic, arachidonic, .. Omega6Parts]);

        // RAE = retinol + β-carotene/12 + (α-carotene + β-cryptoxanthin)/24 (công thức của IOM/USDA); vitamin A chỉ có IU (318) thì để chưa biết (tỉ lệ IU→RAE phụ thuộc nguồn)
        if (!profile.ContainsKey("vitamin_a_mcg") && (profile.ContainsKey("retinol_mcg") || profile.ContainsKey("carotene_beta_mcg")))
        {
            decimal Get(string code) => profile.TryGetValue(code, out var v) ? v.Amount : 0m;
            profile["vitamin_a_mcg"] = new NutrientValue(
                Get("retinol_mcg") + Get("carotene_beta_mcg") / 12m + (Get("carotene_alpha_mcg") + Get("cryptoxanthin_beta_mcg")) / 24m, NutrientAmountStatus.Estimated);
        }
    }

    private static void Fallback(Dictionary<string, NutrientValue> profile, string code, string source)
    {
        if (!profile.ContainsKey(code) && profile.TryGetValue(source, out var v)) profile[code] = new NutrientValue(v.Amount, NutrientAmountStatus.Estimated);
    }

    private static void Sum(Dictionary<string, NutrientValue> profile, string code, IReadOnlyList<string> parts)
    {
        if (profile.ContainsKey(code)) return;
        var known = parts.Where(profile.ContainsKey).ToList();
        if (known.Count > 0) profile[code] = new NutrientValue(known.Sum(p => profile[p].Amount), NutrientAmountStatus.Estimated);
    }
}
