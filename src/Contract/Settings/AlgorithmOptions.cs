namespace Contract;

/// <summary>Hằng số thuật toán MacroFactor-like (mục "Algorithm" trong appsettings). Đổi để hiệu chỉnh, không sửa code.</summary>
public sealed class AlgorithmOptions
{
    public const string SectionName = "Algorithm";

    /// <summary>kcal cho 1 kg thay đổi cân nặng; đối xứng tăng/giảm (V3).</summary>
    public int KcalPerKg { get; init; } = 7700;
    /// <summary>Cửa sổ EMA trend cân nặng (ngày).</summary>
    public int TrendDays { get; init; } = 20;
    /// <summary>Cửa sổ tính expenditure (ngày).</summary>
    public int ExpenditureWindowDays { get; init; } = 21;
    /// <summary>Số ngày dữ liệu tối thiểu trước khi rời Calibrating.</summary>
    public int CalibratingDays { get; init; } = 14;
    /// <summary>Giới hạn thay đổi kcal mỗi check-in (hedge).</summary>
    public int MaxWeeklyDeltaKcal { get; init; } = 150;
    /// <summary>Hệ số làm mượt TDEE ngày-qua-ngày.</summary>
    public decimal SmoothingBeta { get; init; } = 0.1m;
}
