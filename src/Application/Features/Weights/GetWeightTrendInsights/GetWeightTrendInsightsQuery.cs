namespace Application;

/// <summary>
/// Insights &amp; Data: Weight Changes 3/7/14/30/90 ngày (trend hôm nay − trend N ngày trước, Waiting nếu chưa đủ dữ liệu), cân hiện tại (trend hôm nay),
/// thay đổi hằng tuần (hồi quy tuyến tính trend 21 ngày gần nhất), thâm hụt năng lượng (× Algorithm:KcalPerKg ÷ 7) và cân dự kiến sau 30 ngày. Tính đến hôm nay theo múi giờ user.
/// </summary>
public sealed record GetWeightTrendInsightsQuery : IRequest<WeightTrendInsightsResponse>;
