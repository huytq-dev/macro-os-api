namespace Application;

/// <summary>
/// Chính sách làm tròn dinh dưỡng, một nơi duy nhất. Ba tầng:
/// (1) tính toán nội bộ: decimal, không làm tròn giữa chừng;
/// (2) snapshot lưu DB: <see cref="Macro"/> / <see cref="Micro"/> số lẻ, giữ nguyên để số liệu đã lưu không đổi;
/// (3) hiển thị: theo <c>NutrientDefinition.DisplayPrecision</c> ở lớp trình bày, hoặc phản hồi gọn (<see cref="CompactKcal"/>/<see cref="CompactMacro"/>).
/// Chế độ làm tròn là ToEven, đúng mặc định của <c>Math.Round</c> mà mọi snapshot hiện có đã dùng; đổi chế độ sẽ làm lệch số cũ.
/// </summary>
public static class NutritionRounding
{
    public const MidpointRounding Mode = MidpointRounding.ToEven;

    /// <summary>Cột snapshot của FoodLogEntry (kcal/P/C/F/fiber/sodium), tổng và khẩu phần recipe.</summary>
    public const int Macro = 2;
    /// <summary>FoodNutrient / FoodLogEntryNutrient và API chất theo món/entry.</summary>
    public const int Micro = 4;
    public const int Percent = 1;
    /// <summary>Gram nguyên liệu recipe.</summary>
    public const int Grams = 2;
    /// <summary>Phản hồi gọn: kcal số nguyên, macro 1 số lẻ (yêu thích, ước lượng AI).</summary>
    public const int CompactKcal = 0, CompactMacro = 1;

    public static decimal Round(decimal value, int decimals) => Math.Round(value, decimals, Mode);

    public static decimal? Round(decimal? value, int decimals) => value is { } v ? Round(v, decimals) : null;
}
