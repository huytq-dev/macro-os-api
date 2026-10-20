namespace Contract;

/// <summary>Độ gộp của biểu đồ (chip D ▾): mỗi bucket là trung bình các điểm trong bucket.</summary>
public enum MetricGranularity { Day, Week, Month }

/// <summary>Thống kê một khoảng. Average = trung bình mọi điểm trong khoảng; Difference = điểm cuối trừ điểm đầu (null nếu &lt; 2 điểm); không có điểm → Count 0 và các giá trị null.</summary>
/// <param name="From">Đầu khoảng hiệu lực (khi gửi all=true và không có from: ngày có dữ liệu đầu tiên).</param>
/// <param name="To">Cuối khoảng hiệu lực (mặc định hôm nay theo múi giờ user).</param>
public sealed record MetricSummary(
    DateOnly From,
    DateOnly To,
    int Count,
    decimal? Average,
    decimal? Difference,
    decimal? First,
    decimal? Last,
    decimal? Min,
    decimal? Max
);

/// <summary>Một bucket của biểu đồ: Start..End (Day: một ngày; Week: 7 ngày từ ngày đầu tuần của user; Month: cả tháng), chỉ có bucket chứa dữ liệu.</summary>
public sealed record MetricBucket(DateOnly Start, DateOnly End, int Count, decimal Average);

public sealed record VisualBodyFatEntryModel(Guid Id, DateOnly Date, decimal Percent);

/// <param name="Items">Mới nhất trước, mỗi ngày một dòng.</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
/// <param name="LatestPercent">Lần ghi mới nhất trong khoảng; null nếu khoảng rỗng.</param>
public sealed record VisualBodyFatResponse(
    IReadOnlyList<VisualBodyFatEntryModel> Items,
    MetricSummary Summary,
    IReadOnlyList<MetricBucket> Buckets,
    decimal? LatestPercent
);
