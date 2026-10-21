namespace Application;

/// <summary>Snapshot đã lưu của một ngày (đầu vào thuần của <see cref="ExpenditureSeries"/>).</summary>
public sealed record ExpenditureSnapshotRow(DateOnly Date, decimal TdeeKcal, string Status, int WindowDays);
