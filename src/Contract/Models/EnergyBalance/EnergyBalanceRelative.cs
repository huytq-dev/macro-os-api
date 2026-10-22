namespace Contract;

/// <param name="Days">3 | 7 | 14 | 30 | 90: cửa sổ N ngày kết thúc HÔM NAY (gồm hôm nay).</param>
/// <param name="AvgKcal">Trung bình chênh lệch mỗi ngày đã log hợp lệ trong cửa sổ (âm = Deficit / BelowTarget); null khi Waiting.</param>
/// <param name="LoggedDays">Số ngày đã dùng để tính AvgKcal.</param>
/// <param name="Status">Expenditure: Deficit | Surplus | Balanced | Waiting. Targets: BelowTarget | AboveTarget | OnTarget | Waiting. Waiting = chưa có ngày nào dùng được trong cửa sổ.</param>
/// <param name="UnverifiedDays">Chỉ RelativeToTargets: số ngày hợp lệ trong cửa sổ có target LEGACY_UNVERIFIED, đã LOẠI khỏi AvgKcal/LoggedDays/Status (Waiting nếu chỉ còn ngày chưa xác minh); luôn 0 với Expenditure.</param>
public sealed record EnergyBalanceRelative(int Days, decimal? AvgKcal, int LoggedDays, string Status, int UnverifiedDays = 0);
