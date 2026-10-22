namespace Contract;

/// <summary>Hằng số thuật toán MacroFactor-like (mục "Algorithm" trong appsettings). Đổi để hiệu chỉnh, không sửa code.</summary>
public sealed class AlgorithmOptions
{
    public const string SectionName = "Algorithm";

    /// <summary>kcal cho 1 kg thay đổi cân nặng; đối xứng tăng/giảm (V3).</summary>
    public int KcalPerKg { get; init; } = 7700;
    /// <summary>
    /// Ngân sách số dòng snapshot ghi trong lúc dựng ĐỒNG BỘ sau khi sửa dữ liệu nguồn (trước khi API trả về). Quá ngân sách thì phần còn lại thành job nền bền (RecalculationJob).
    /// Đo trên 730 ngày dữ liệu: sửa cân 2 kg lệch ≥ 1 kcal tới ~83 ngày ≈ 150–190 dòng. 0 = không dựng đồng bộ, mọi việc đi qua job nền.
    /// </summary>
    public int DerivedSyncRowBudget { get; init; } = 300;
    /// <summary>Thời gian chờ khoá dựng lại theo user (ms) ở request; không lấy được khoá thì xếp job nền.</summary>
    public int DerivedLockWaitMs { get; init; } = 3000;
    /// <summary>Thời gian chờ khoá (ms) của worker nền; không lấy được thì trả job về Pending (không tính lần thử).</summary>
    public int DerivedWorkerLockWaitMs { get; init; } = 10000;
    /// <summary>Số lần thử tối đa của một job nền trước khi thành Failed (FE thấy Failed, user/ops có thể thử lại).</summary>
    public int DerivedJobMaxAttempts { get; init; } = 5;
    /// <summary>Giãn cách thử lại cơ sở (giây); lần n chờ cơ sở × 2^(n−1).</summary>
    public int DerivedJobRetryBaseSeconds { get; init; } = 10;
    /// <summary>Job Running quá thời gian này (giây) coi là instance đã chết/restart giữa chừng và được nhận lại.</summary>
    public int DerivedJobLeaseSeconds { get; init; } = 300;
    /// <summary>Chu kỳ worker quét job (giây); ngoài ra worker được đánh thức ngay khi instance này xếp job.</summary>
    public int DerivedWorkerPollSeconds { get; init; } = 10;
    /// <summary>Cửa sổ EMA trend cân nặng (ngày).</summary>
    public int TrendDays { get; init; } = 20;
    /// <summary>Cửa sổ tính expenditure (ngày).</summary>
    public int ExpenditureWindowDays { get; init; } = 21;
    /// <summary>Số ngày của giai đoạn ước lượng ban đầu (nhãn Calibrating; và độ dài đóng băng TDEE khi <see cref="FreezeTdeeDuringCalibration"/> = true).</summary>
    public int CalibratingDays { get; init; } = 14;
    /// <summary>
    /// true (mặc định, hành vi trước #3) = TDEE bị giữ nguyên trong <see cref="CalibratingDays"/> ngày đầu (status Calibrating). false = "học từ ngày 1" (#3, ĐANG CHỜ user duyệt phát hành):
    /// cập nhật ngay khi đủ điều kiện Updating. Đổi cờ này đổi nhãn phiên bản ghi vào snapshot (v2 = đóng băng, v3 = học từ ngày 1), xem <c>AlgorithmVersions.ExpenditureFor</c>.
    /// </summary>
    public bool FreezeTdeeDuringCalibration { get; init; } = true;
    /// <summary>
    /// true (mặc định) = chạy worker dựng lại nền (RecalculationJob) và job đêm. false = KHÔNG chạy cả hai (dùng khi chạy API cục bộ trỏ vào DB dùng chung): endpoint status vẫn hoạt động,
    /// ghi dữ liệu vẫn dựng đồng bộ phần gần. Biến môi trường: <c>Algorithm__BackgroundDerivedProcessingEnabled=false</c>.
    /// </summary>
    public bool BackgroundDerivedProcessingEnabled { get; init; } = true;
    /// <summary>Giới hạn thay đổi kcal mỗi check-in (hedge).</summary>
    public int MaxWeeklyDeltaKcal { get; init; } = 150;
    /// <summary>Hệ số làm mượt TDEE ngày-qua-ngày.</summary>
    public decimal SmoothingBeta { get; init; } = 0.1m;
    /// <summary>Sai số giả định của trend cân nặng (kg) để tính Flux Range: nửa bề rộng = giá trị này × KcalPerKg ÷ cửa sổ. Núm hiệu chỉnh độ rộng dải.</summary>
    public decimal FluxTrendErrorKg { get; init; } = 0.1m;

    // --- Phân loại tốc độ thay đổi cân so với mục tiêu (ON_TARGET / FASTER / SLOWER / UNCERTAIN / WAITING). MACROOS POLICY, KHÔNG phải MacroFactor; INACTIVE: chưa có caller production ---
    /// <summary>δ: độ lệch chấp nhận được so với tốc độ mục tiêu (kg/tuần). Khái niệm KHÁC với độ bất định đo lường (k·SE).</summary>
    public decimal RateOnTargetDeltaKgPerWeek { get; init; } = 0.10m;
    /// <summary>k: hệ số tin cậy của khoảng bất định (1.645 = hai phía 90 % nếu sai số Gaussian). Không phải của MacroFactor.</summary>
    public decimal RateOnTargetConfidenceK { get; init; } = 1.645m;
    /// <summary>λ: hệ số nới SE đã hiệu chỉnh bằng mô phỏng (độ phủ ≥ 0.89 ở nhóm tệ nhất, gồm tự tương quan nước/glycogen).</summary>
    public decimal RateSeInflation { get; init; } = 1.3m;
    /// <summary>|φ| tối đa (sai lệch khởi động của trend EMA, tính từ NGÀY cân) để còn phân loại; lớn hơn ⇒ UNCERTAIN. ≈ chuỗi ≥ 4 tuần. ĐỀ XUẤT, cần duyệt.</summary>
    public decimal RateMaxWarmupBias { get; init; } = 0.15m;
    /// <summary>Công tắc: false (mặc định) = không có luồng production nào gọi bộ phân loại.</summary>
    public bool RateClassificationEnabled { get; init; } = false;
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
