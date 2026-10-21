namespace Application;

/// <summary>
/// Dashboard: danh sách các số đo vòng ĐÃ có dữ liệu, mỗi số đo kèm giá trị mới nhất và <c>Entries</c> lần đo gần nhất (mặc định 7, tối đa 30; mới nhất trước, mỗi ngày một giá trị).
/// Số đo chưa từng có dữ liệu không xuất hiện. Cân nặng và % mỡ Visual có API riêng (GET /weights?limit=7, GET /body-metrics/visual-body-fat?limit=7).
/// </summary>
public sealed record GetMeasurementOverviewQuery(int? Entries = null) : IRequest<MeasurementOverviewResponse>;
