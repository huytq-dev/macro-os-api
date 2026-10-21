namespace Application;

/// <summary>
/// Một số đo vòng (cm) của hàng Tape trong BodyMetric: khoá API, nhóm hiển thị, khoảng hợp lý và cách đọc/ghi cột tương ứng.
/// Full Body (cả sheet) và các trang từng số đo dùng CHUNG danh mục này nên chỉ có một nguồn dữ liệu.
/// </summary>
/// <param name="ModelName">Tên trường trong <see cref="TapeMeasurementsModel"/> (dùng cho thông báo lỗi).</param>
public sealed record BodyMeasurement(
    string Key, string Group, string ModelName, decimal Min, decimal Max,
    Func<BodyMetric, decimal?> Get, Action<BodyMetric, decimal?> Set, Func<TapeMeasurementsModel, decimal?> FromModel);
