namespace Application;

/// <summary>
/// Insights &amp; Data của trang Expenditure: TDEE hôm nay, Strategy (Calibrating | Updating | Holding) và lý do Holding (thiếu log / không cân trong 7 ngày),
/// thay đổi so với 3/7/14/30/90 ngày trước (Waiting khi mốc nằm trước ngày đầu của chuỗi). Tính đến hôm nay theo múi giờ user.
/// </summary>
public sealed record GetExpenditureInsightsQuery : IRequest<ExpenditureInsightsResponse>;
