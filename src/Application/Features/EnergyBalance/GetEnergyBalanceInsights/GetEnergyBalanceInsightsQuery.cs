namespace Application;

/// <summary>
/// Insights &amp; Data của Energy Balance: Relative to Expenditure và Relative to Targets cho 3/7/14/30/90 ngày kết thúc hôm nay (theo múi giờ user): trung bình chênh lệch mỗi ngày đã log
/// hợp lệ trong cửa sổ, nhãn Deficit/Surplus/Balanced và BelowTarget/AboveTarget/OnTarget; Waiting khi cửa sổ chưa có ngày nào dùng được.
/// </summary>
public sealed record GetEnergyBalanceInsightsQuery : IRequest<EnergyBalanceInsightsResponse>;
