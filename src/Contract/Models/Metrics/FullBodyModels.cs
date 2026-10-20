namespace Contract;

/// <summary>
/// Số đo vòng cơ thể (cm), mọi ô tuỳ chọn. Tên UI → trường: Bicep = <c>LeftArmCm/RightArmCm</c>, Hips = <c>HipCm</c>.
/// </summary>
public sealed record TapeMeasurementsModel(
    decimal? NeckCm,
    decimal? ShouldersCm,
    decimal? BustCm,
    decimal? ChestCm,
    decimal? WaistCm,
    decimal? HipCm,
    decimal? LeftArmCm,
    decimal? RightArmCm,
    decimal? LeftForearmCm,
    decimal? RightForearmCm,
    decimal? LeftWristCm,
    decimal? RightWristCm,
    decimal? LeftThighCm,
    decimal? RightThighCm,
    decimal? LeftCalfCm,
    decimal? RightCalfCm,
    decimal? LeftAnkleCm,
    decimal? RightAnkleCm
)
{
    /// <summary>Có ít nhất một ô được nhập (dùng để quyết định giữ hay xoá hàng Tape).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool HasAnyValue => GetType().GetProperties().Any(p => p.PropertyType == typeof(decimal?) && p.GetValue(this) is not null);
}

/// <summary>
/// Sheet Metrics (Full Body) của một ngày: % mỡ Visual (hàng <c>Method=Visual</c>) + số đo vòng (hàng <c>Method=Tape</c>).
/// </summary>
/// <param name="Tape">null khi ngày đó chưa có số đo vòng nào.</param>
/// <param name="HasData">false khi cả % mỡ lẫn số đo đều trống.</param>
/// <param name="ScaleWeightKg">Cân nặng của đúng ngày này nếu có (không tính vào HasData).</param>
/// <param name="TrendWeightKg">Trend cân nặng của ngày này nếu tính được.</param>
public sealed record FullBodyResponse(DateOnly Date, decimal? VisualBodyFatPercent, TapeMeasurementsModel? Tape, bool HasData, decimal? ScaleWeightKg = null, decimal? TrendWeightKg = null);

/// <param name="MetricCount">Số chỉ số có giá trị của ngày: % mỡ Visual (nếu có) + số ô số đo vòng có giá trị.</param>
/// <param name="Tape">null khi ngày đó không có số đo vòng.</param>
public sealed record FullBodyHistoryItem(DateOnly Date, int MetricCount, decimal? VisualBodyFatPercent, TapeMeasurementsModel? Tape, decimal? ScaleWeightKg, decimal? TrendWeightKg);

/// <param name="Items">Chỉ các ngày có dữ liệu, mới nhất trước.</param>
public sealed record FullBodyHistoryResponse(IReadOnlyList<FullBodyHistoryItem> Items);

/// <summary>Một chỉ số trong so sánh hai ngày. Delta = ValueB − ValueA (null nếu thiếu một bên).</summary>
/// <param name="Key">scaleWeight | trendWeight | visualBodyFat | khoá số đo vòng (neck, waist, leftBicep…).</param>
/// <param name="Group">WeightAndBodyFat | UpperBody | Arms | Legs.</param>
/// <param name="Trend">Up | Down | Same; null khi không có Delta.</param>
/// <param name="Quality">Good | Bad | Neutral theo mục tiêu của user (giảm cân: giảm là Good; tăng cân/tăng cơ: tăng là Good; còn lại Neutral; % mỡ giảm luôn Good); null khi không có Delta.</param>
public sealed record FullBodyCompareItem(string Key, string Group, decimal? ValueA, decimal? ValueB, decimal? Delta, string? Trend, string? Quality);

/// <param name="Goal">Decrease | Increase | Neutral: hướng Good theo program đang chạy của user (không có program = Neutral).</param>
public sealed record FullBodyCompareResponse(DateOnly DateA, DateOnly DateB, string Goal, IReadOnlyList<FullBodyCompareItem> Items);

/// <summary>Một lần đo của một số đo vòng, đơn vị cm (FE đổi sang in khi hiển thị).</summary>
public sealed record MeasurementEntryModel(DateOnly Date, decimal ValueCm);

/// <param name="Items">Mới nhất trước, mỗi ngày một giá trị (có thể bị cắt bởi limit).</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
/// <param name="LatestCm">Lần đo mới nhất (cả ngoài khoảng); null nếu chưa có.</param>
public sealed record MeasurementSeriesResponse(string Key, string Group, IReadOnlyList<MeasurementEntryModel> Items, MetricSummary Summary, IReadOnlyList<MetricBucket> Buckets, decimal? LatestCm);

public sealed record MeasurementOverviewItem(string Key, string Group, DateOnly LatestDate, decimal LatestCm, IReadOnlyList<MeasurementEntryModel> Entries);

/// <param name="Metrics">Chỉ các số đo đã có dữ liệu, theo thứ tự Neck…Right Ankle.</param>
public sealed record MeasurementOverviewResponse(IReadOnlyList<MeasurementOverviewItem> Metrics);
