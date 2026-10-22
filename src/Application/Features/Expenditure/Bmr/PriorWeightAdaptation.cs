namespace Application;

/// <summary>
/// Quy tắc bảo thủ (MACROOS, dựa trên điều kiện đã công bố của MF, không phải nội bộ MF): ">10% dưới đỉnh" chỉ được XÁC LẬP khi đáp án = Yes VÀ ngưỡng user thực sự được hỏi
/// (số chính xác đã lưu, và số hiển thị đã lưu quy ra kg) ≥ cân lúc trả lời / 0.9: khi đó "từng nặng hơn ngưỡng" kéo theo đỉnh &gt; cân/0.9. Ngưỡng thấp hơn ⇒ Unknown (không ×0.97);
/// thiếu ngưỡng/cân lúc trả lời ⇒ Unknown, không dựng lại. Không bao giờ đánh giá lại đáp án cũ theo cân mới; bằng chứng mới (cân đỉnh thật) mới tạo một lần đánh giá mới.
/// </summary>
public static class PriorWeightAdaptation
{
    public static MetabolicAdaptationEligibility Eligibility(
        WeightHistoryAnswer? answer, decimal? exactThresholdKg, decimal? shownValue, string? shownUnit, decimal? atWeightKg)
    {
        if (answer != WeightHistoryAnswer.Yes) return MetabolicAdaptationEligibility.NotApplicable;
        if (exactThresholdKg is not { } exact || atWeightKg is not { } weight || weight <= 0) return MetabolicAdaptationEligibility.Unknown;

        // Điều kiện dùng ngưỡng THẤP HƠN giữa số chính xác và số hiển thị quy ra kg: nếu số hiển thị bị làm tròn xuống, câu hỏi thực tế yếu hơn
        var asked = shownValue is { } shown ? Math.Min(exact, PriorWeightQuestion.ToKg(shown, shownUnit)) : exact;
        return asked >= PriorWeightQuestion.ExactKg(weight) ? MetabolicAdaptationEligibility.Established : MetabolicAdaptationEligibility.Unknown;
    }
}
