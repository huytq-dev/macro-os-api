namespace Contract;

/// <param name="Days">3 | 7 | 14 | 30 | 90.</param>
/// <param name="ChangeKg">Trend hôm nay trừ trend N ngày trước; null khi chưa đủ dữ liệu (Status = Waiting).</param>
/// <param name="Status">Decrease | Increase | NoChange (|thay đổi| &lt; 0,05 kg) | Waiting.</param>
public sealed record WeightChangeModel(int Days, decimal? ChangeKg, string Status);
