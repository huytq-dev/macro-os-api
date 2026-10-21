namespace Contract;

/// <param name="Metrics">Chỉ các số đo đã có dữ liệu, theo thứ tự Neck…Right Ankle.</param>
public sealed record MeasurementOverviewResponse(IReadOnlyList<MeasurementOverviewItem> Metrics);
