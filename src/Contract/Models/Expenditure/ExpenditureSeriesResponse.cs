namespace Contract;

/// <summary>Trang Expenditure: chuỗi TDEE mỗi ngày trong khoảng + thống kê + bucket.</summary>
/// <param name="From">Đầu khoảng hiệu lực (all=true không gửi from: ngày đầu tiên của chuỗi).</param>
/// <param name="To">Cuối khoảng hiệu lực (không vượt hôm nay theo múi giờ user).</param>
/// <param name="Days">Thời gian TĂNG dần, liên tục; rỗng nếu chưa có snapshot nào.</param>
/// <param name="Summary">Thống kê trên ExpenditureKcal các ngày trong khoảng: Average, Difference = ngày cuối − ngày đầu.</param>
/// <param name="Buckets">Rỗng khi không gửi granularity. Mỗi bucket kèm dải Flux gộp theo chính sách ở <see cref="ExpenditureBucketModel"/>.</param>
/// <param name="FluxModel">Phiên bản chính sách Flux đã tính ra các dải (<c>MACROOS_FLUX_V1</c>); null khi không ngày nào có dải. **Mặc định LUÔN null** (Algorithm:FluxRangeEnabled = false, user giữ Flux ẩn tới khi duyệt validation mới): FE không vẽ dải khi null. Dải là MacroOS APPROXIMATION: MacroFactor chỉ định nghĩa flux range là "navigable space" ("for fun and curiosity", không phải khoảng tin cậy) và KHÔNG công bố công thức; đây là "ước lượng bất đồng giữa các độ nhạy" của MacroOS, KHÔNG phải công thức MF.</param>
public sealed record ExpenditureSeriesResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<ExpenditureSeriesDay> Days,
    MetricSummary Summary,
    IReadOnlyList<ExpenditureBucketModel> Buckets,
    string? FluxModel = null
);
