namespace Contract;

/// <summary>Hằng số thuật toán MacroFactor-like (mục "Algorithm" trong appsettings). Đổi để hiệu chỉnh, không sửa code.</summary>
public sealed class AlgorithmOptions
{
    public const string SectionName = "Algorithm";

    /// <summary>kcal cho 1 kg thay đổi cân nặng; đối xứng tăng/giảm (V3).</summary>
    public int KcalPerKg { get; init; } = 7700;
    /// <summary>
    /// true (mặc định) = dựng lại aggregate dinh dưỡng + snapshot expenditure SAU khi đã trả phản hồi (POST entries/weights nhanh hơn nhiều trên DB ở xa);
    /// false = dựng đồng bộ trong request như trước.
    /// </summary>
    public bool RebuildDerivedInBackground { get; init; } = true;
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
    /// <summary>Sai số giả định của trend cân nặng (kg) để tính Flux Range: nửa bề rộng = giá trị này × KcalPerKg ÷ cửa sổ. Núm hiệu chỉnh độ rộng dải.</summary>
    public decimal FluxTrendErrorKg { get; init; } = 0.1m;
    /// <summary>Ngày đã log nhưng kcal &lt; tỉ lệ này × TDEE của ngày đó thì check-in gợi ý "có thể log chưa đủ".</summary>
    public decimal IncompleteDayKcalRatio { get; init; } = 0.5m;
    /// <summary>Trần kcal/ngày user được đặt ở wizard program = hệ số này × TDEE (làm tròn 100, tối thiểu 3000).</summary>
    public decimal DayKcalMaxFactor { get; init; } = 2m;
    /// <summary>Trần protein (g/kg cân nặng) ở editor theo ngày.</summary>
    public decimal MaxProteinGPerKg { get; init; } = 4m;
    /// <summary>Dải tốc độ giảm cân cho Edit Goal (% cân nặng/tuần): Slow ≤ SlowMax &lt; Standard ≤ StandardMax &lt; Fast ≤ FastMax (tối đa cho phép).</summary>
    public decimal LossRateSlowMaxPercent { get; init; } = 0.35m;
    public decimal LossRateStandardMaxPercent { get; init; } = 0.7m;
    public decimal LossRateFastMaxPercent { get; init; } = 1m;
    /// <summary>Dải tốc độ tăng cân (chậm hơn giảm cân để hạn chế tăng mỡ).</summary>
    public decimal GainRateSlowMaxPercent { get; init; } = 0.15m;
    public decimal GainRateStandardMaxPercent { get; init; } = 0.3m;
    public decimal GainRateFastMaxPercent { get; init; } = 0.5m;
}
