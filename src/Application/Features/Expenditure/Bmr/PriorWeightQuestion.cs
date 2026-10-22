namespace Application;

/// <summary>
/// Ngưỡng "X" của câu hỏi Prior Weight ("Have you ever weighed more than X?") — CHÍNH SÁCH TẠM THỜI CỦA MACROOS (<see cref="Policy"/>), KHÔNG phải công thức hiển thị đã xác minh của MF
/// (H1: cân / 0.9; H2 cân × 1.06 và H3 cân + 5 kg chưa loại, xem Docs/Design/Prior_Weight_Threshold_Evidence.md). Một hàm duy nhất ở BE: FE và BE dùng chung số này.
/// Ngưỡng CHÍNH XÁC (kg, làm tròn LÊN 4 chữ số) là giá trị dùng để xét điều kiện; số HIỂN THỊ làm tròn lên số nguyên theo đơn vị user ⇒ trả lời Yes với số hiển thị luôn kéo theo Yes với ngưỡng chính xác.
/// Đây là ngưỡng CÂU HỎI, tách khỏi điều kiện thích nghi chuyển hoá (xem <see cref="PriorWeightAdaptation"/>).
/// </summary>
public static class PriorWeightQuestion
{
    public const string Policy = "MACROOS_PROVISIONAL_POLICY";
    public const decimal Ratio = 0.9m;
    public const decimal PoundsPerKg = 2.2046226218m;

    /// <summary>Ngưỡng chính xác (kg), làm tròn lên 4 chữ số thập phân để không bao giờ thấp hơn cân/0.9.</summary>
    public static decimal ExactKg(decimal weightKg) => Math.Ceiling(weightKg / Ratio * 10000m) / 10000m;

    /// <summary>Số hiển thị: số nguyên làm tròn LÊN của ngưỡng chính xác, theo đơn vị ("kg" | "lb"); đơn vị khác ⇒ kg.</summary>
    public static (decimal Value, string Unit) Display(decimal exactKg, string? unit)
        => string.Equals(unit, "lb", StringComparison.OrdinalIgnoreCase) ? (Math.Ceiling(exactKg * PoundsPerKg), "lb") : (Math.Ceiling(exactKg), "kg");

    public static decimal ToKg(decimal value, string? unit) => string.Equals(unit, "lb", StringComparison.OrdinalIgnoreCase) ? value / PoundsPerKg : value;
}
