namespace Contract;

/// <param name="Days">Mốc so sánh: 3, 7, 14, 30 hoặc 90 ngày trước.</param>
/// <param name="DeltaKcal">TDEE hiện tại − TDEE tại mốc; null khi Waiting.</param>
/// <param name="Status">Ready | Waiting (chưa có điểm ở mốc đó).</param>
public sealed record ExpenditureChangeModel(int Days, decimal? DeltaKcal, string Status)
{
    public const string Ready = "Ready";
    public const string Waiting = "Waiting";
}
