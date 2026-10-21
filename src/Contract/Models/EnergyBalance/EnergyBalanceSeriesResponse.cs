namespace Contract;

/// <param name="From">Đầu khoảng hiệu lực (all=true không gửi from: ngày có dữ liệu đầu tiên).</param>
/// <param name="To">Cuối khoảng hiệu lực (không vượt hôm nay theo múi giờ user).</param>
/// <param name="Days">Thời gian TĂNG dần, LIÊN TỤC từ From đến To (ngày không có dữ liệu có các giá trị null).</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
public sealed record EnergyBalanceSeriesResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<EnergyBalanceDay> Days,
    EnergyBalanceSummary Summary,
    IReadOnlyList<EnergyBalanceBucket> Buckets
);
