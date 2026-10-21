namespace Application;

/// <summary>
/// Trang riêng của MỘT số đo vòng (Neck, Waist, Left Bicep…; khoá trong <see cref="BodyMeasurements.All"/>): các lần đo trong khoảng (mới nhất trước, mỗi ngày một giá trị, cm),
/// thống kê khoảng (Average, Difference = cuối − đầu) và bucket Day/Week/Month — cùng luật khoảng như GET /weights. Dữ liệu là cột của hàng Tape: chung nguồn với Full Body.
/// <c>Limit</c> chỉ cắt danh sách <c>Items</c> (Summary/Buckets vẫn tính trên cả khoảng), dùng cho thẻ Dashboard "last 7 entries".
/// </summary>
public sealed record GetMeasurementSeriesQuery(string Key, DateOnly? From, DateOnly? To, MetricGranularity? Granularity = null, bool? All = null, int? Limit = null)
    : IRequest<MeasurementSeriesResponse>;
