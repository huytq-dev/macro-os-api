namespace Application;

/// <summary>Ghi MỘT đáp án Prior Weight vào hồ sơ cùng ngữ cảnh lúc trả lời (cân, ngưỡng chính xác, số hiển thị + đơn vị). Chỉ gọi khi user trả lời; không bao giờ gọi lại để tính lại đáp án cũ.</summary>
public static class PriorWeightAnswer
{
    /// <param name="weightAtAnswerKg">Cân lúc trả lời (null nếu chưa có cân nào: không lưu ngưỡng, không bịa).</param>
    /// <param name="shownValue">Số FE đã hiển thị cho user (nếu có); null ⇒ BE dùng số hiển thị của chính sách theo <paramref name="fallbackUnit"/>.</param>
    public static void Apply(UserProfile profile, WeightHistoryAnswer answer, decimal? weightAtAnswerKg, decimal? shownValue, string? shownUnit, UnitSystem fallbackUnit)
    {
        profile.WeighedMoreThanBefore = answer;
        profile.WeighedMoreThanBeforeAtWeightKg = weightAtAnswerKg;
        if (weightAtAnswerKg is not { } weight) { profile.WeighedMoreThanBeforeThresholdKg = null; profile.WeighedMoreThanBeforeThresholdShown = null; profile.WeighedMoreThanBeforeThresholdUnit = null; return; }

        var exact = PriorWeightQuestion.ExactKg(weight);
        profile.WeighedMoreThanBeforeThresholdKg = exact;
        if (shownValue is { } shown)
        {
            profile.WeighedMoreThanBeforeThresholdShown = shown;
            profile.WeighedMoreThanBeforeThresholdUnit = string.Equals(shownUnit, "lb", StringComparison.OrdinalIgnoreCase) ? "lb" : "kg";
        }
        else
        {
            var (value, unit) = PriorWeightQuestion.Display(exact, fallbackUnit == UnitSystem.Imperial ? "lb" : "kg");
            profile.WeighedMoreThanBeforeThresholdShown = value;
            profile.WeighedMoreThanBeforeThresholdUnit = unit;
        }
    }
}
