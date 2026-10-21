namespace Contract;

/// <param name="Days">3 | 7 | 14 | 30 | 90.</param>
/// <param name="ChangeKcal">TDEE hôm nay trừ TDEE N ngày trước; null khi Waiting.</param>
/// <param name="Status">Decrease | Increase | NoChange (thay đổi làm tròn = 0) | Waiting (mốc trước ngày đầu của chuỗi).</param>
public sealed record ExpenditureInsightChange(int Days, decimal? ChangeKcal, string Status);
