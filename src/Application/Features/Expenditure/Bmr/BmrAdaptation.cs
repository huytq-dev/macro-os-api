namespace Application;

/// <summary>
/// Điều chỉnh thích nghi cho BMR theo MacroFactor (hằng số đã xác minh): ×0.95 khi hiện đang thâm hụt; nếu cân hiện tại &lt; 0.9 × cân CAO NHẤT: đang giảm ×0.92, ổn định/tăng ×0.97.
/// Nguồn đã có KHÔNG nêu cách kết hợp khi cả hai điều kiện cùng đúng; cài đặt này coi chúng loại trừ nhau (đã giảm ≥ 10% so với đỉnh thay thế hệ số thâm hụt; "đang giảm" lấy từ Prior Progress, không phải từ thâm hụt hiện tại). CẦN đối chiếu văn bản gốc trước khi dùng mặc định.
/// </summary>
public static class BmrAdaptation
{
    public const decimal Deficit = 0.95m;
    public const decimal ReducedAndLosing = 0.92m;
    public const decimal ReducedAndStableOrGaining = 0.97m;
    public const decimal ReducedWeightRatio = 0.9m;

    /// <summary>Cân hiện tại &lt; 0.9 × cân cao nhất. Dùng khi biết cân đỉnh bằng kg; wizard chỉ hỏi định tính (<see cref="WeightHistoryAnswer"/>) nên thường dùng <c>reducedFromPeak</c> trực tiếp.</summary>
    public static bool ReducedFromPeak(decimal currentWeightKg, decimal highestWeightKg)
        => highestWeightKg > 0 && currentWeightKg < ReducedWeightRatio * highestWeightKg;

    /// <summary>
    /// Điều kiện ">10% dưới đỉnh" lấy từ <see cref="PriorWeightAdaptation.Eligibility"/> (quy tắc bảo thủ: chỉ Established mới áp ×0.97/×0.92; Unknown/NotApplicable ⇒ không điều chỉnh theo đỉnh, kể cả Prior Weight = Yes với ngưỡng thấp).
    /// Trạng thái "đang giảm/thâm hụt" (QUYẾT ĐỊNH C: không phát hiện thâm hụt từ dữ liệu): caller truyền <paramref name="inDeficit"/> = null ⇒ dùng Prior Progress = Losing làm proxy cho BMR khởi điểm (MACROOS POLICY, không phải MF).
    /// Mục tiêu giảm cân (GoalType) KHÔNG BAO GIỜ được coi là thâm hụt; 0.95 và 0.97 không bao giờ nhân với nhau (0.92 là hệ số riêng khi cả hai điều kiện đúng).
    /// Chỉ áp MỘT LẦN lên BMR khởi điểm, không bao giờ đụng expenditure đã học.
    /// </summary>
    public static decimal Factor(MetabolicAdaptationEligibility peak, WeightTrendAnswer? priorProgress, bool? inDeficit)
    {
        var losingNow = inDeficit ?? priorProgress == WeightTrendAnswer.Losing;
        return Factor(peak == MetabolicAdaptationEligibility.Established, losingNow, losingNow);
    }

    /// <param name="reducedFromPeak">Đã giảm ≥ 10% so với đỉnh.</param>
    /// <param name="recentlyLosing">Cân đang giảm (Prior Progress = Losing); chỉ dùng khi <paramref name="reducedFromPeak"/>.</param>
    /// <param name="inDeficit">Hiện đang thâm hụt năng lượng.</param>
    public static decimal Factor(bool reducedFromPeak, bool recentlyLosing, bool inDeficit)
    {
        if (reducedFromPeak) return recentlyLosing ? ReducedAndLosing : ReducedAndStableOrGaining;
        return inDeficit ? Deficit : 1m;
    }
}
