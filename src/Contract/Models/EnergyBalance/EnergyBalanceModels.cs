namespace Contract;

/// <summary>
/// Một ngày của Energy Balance: kcal đã ăn so với expenditure và với target CỦA NGÀY ĐÓ (không phải target hiện tại). Mọi giá trị làm tròn kcal.
/// </summary>
/// <param name="CaloriesKcal">Tổng kcal ngày; null = chưa log; ngày nhịn ăn = 0. Ngày IsIncomplete vẫn có số nhưng không được tính vào thống kê.</param>
/// <param name="ExpenditureKcal">TDEE của ngày (chuỗi expenditure liên tục, ngày chưa có snapshot giữ TDEE gần nhất); null trước ngày đầu của chuỗi.</param>
/// <param name="TargetKcal">Target calories của ngày theo tuần program hiệu lực tại ngày đó (và dạng ngày trong tuần); null trước tuần program đầu tiên / chưa có program.</param>
/// <param name="VsExpenditureKcal">CaloriesKcal − ExpenditureKcal (âm = Deficit, dương = Surplus); null nếu thiếu một trong hai.</param>
/// <param name="VsTargetKcal">CaloriesKcal − TargetKcal (âm = BelowTarget, dương = AboveTarget); null nếu thiếu một trong hai.</param>
public sealed record EnergyBalanceDay(
    DateOnly Date,
    decimal? CaloriesKcal,
    decimal? ExpenditureKcal,
    decimal? TargetKcal,
    decimal? VsExpenditureKcal,
    decimal? VsTargetKcal,
    bool IsFasting,
    bool IsIncomplete
);

/// <summary>
/// Thống kê khoảng. Mọi số trung bình là TRUNG BÌNH MỖI NGÀY ĐÃ LOG trong khoảng (ngày có CaloriesKcal, không IsIncomplete, kể cả nhịn ăn = 0 kcal), không chia cho độ dài khoảng.
/// Trung bình chênh lệch chỉ tính các ngày có cả calories lẫn expenditure (hoặc target).
/// </summary>
/// <param name="LoggedDays">Số ngày đã log hợp lệ trong khoảng.</param>
/// <param name="AvgVsExpenditureKcal">Tab Expenditure: trung bình (calories − expenditure) mỗi ngày đã log (âm = Deficit); null nếu không có ngày nào.</param>
/// <param name="TotalVsExpenditureKcal">Tổng (calories − expenditure) của các ngày đó.</param>
/// <param name="VsExpenditureDays">Số ngày dùng để tính hai số trên.</param>
/// <param name="VsExpenditureStatus">Deficit | Surplus | Balanced (trung bình làm tròn = 0) | Waiting (không có ngày nào).</param>
/// <param name="AvgVsTargetKcal">Tab Calorie Targets: trung bình (calories − target) mỗi ngày đã log (âm = BelowTarget); null nếu không có ngày nào.</param>
/// <param name="VsTargetDays">Số ngày dùng để tính AvgVsTargetKcal.</param>
/// <param name="VsTargetStatus">BelowTarget | AboveTarget | OnTarget | Waiting.</param>
public sealed record EnergyBalanceSummary(
    DateOnly From,
    DateOnly To,
    int LoggedDays,
    decimal? AvgCaloriesKcal,
    decimal? AvgExpenditureKcal,
    decimal? AvgTargetKcal,
    decimal? AvgVsExpenditureKcal,
    decimal? TotalVsExpenditureKcal,
    int VsExpenditureDays,
    string VsExpenditureStatus,
    decimal? AvgVsTargetKcal,
    int VsTargetDays,
    string VsTargetStatus
);

/// <summary>Bucket Day/Week/Month: trung bình các ngày đã log hợp lệ trong bucket (cùng quy tắc với <see cref="EnergyBalanceSummary"/>); chỉ có bucket chứa ít nhất một ngày đã log hợp lệ.</summary>
public sealed record EnergyBalanceBucket(
    DateOnly Start,
    DateOnly End,
    int LoggedDays,
    decimal AvgCaloriesKcal,
    decimal? AvgExpenditureKcal,
    decimal? AvgTargetKcal,
    decimal? AvgVsExpenditureKcal,
    decimal? AvgVsTargetKcal
);

/// <param name="From">Đầu khoảng hiệu lực (all=true không gửi from: ngày có dữ liệu đầu tiên).</param>
/// <param name="To">Cuối khoảng hiệu lực (không vượt hôm nay theo múi giờ user).</param>
/// <param name="Days">Thời gian TĂNG dần, LIÊN TỤC từ From đến To (ngày không có dữ liệu có các giá trị null).</param>
/// <param name="Buckets">Rỗng khi không gửi granularity.</param>
public sealed record EnergyBalanceSeriesResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<EnergyBalanceDay> Days,
    EnergyBalanceSummary Summary,
    IReadOnlyList<EnergyBalanceBucket> Buckets
);

/// <param name="Days">3 | 7 | 14 | 30 | 90: cửa sổ N ngày kết thúc HÔM NAY (gồm hôm nay).</param>
/// <param name="AvgKcal">Trung bình chênh lệch mỗi ngày đã log hợp lệ trong cửa sổ (âm = Deficit / BelowTarget); null khi Waiting.</param>
/// <param name="LoggedDays">Số ngày đã dùng để tính AvgKcal.</param>
/// <param name="Status">Expenditure: Deficit | Surplus | Balanced | Waiting. Targets: BelowTarget | AboveTarget | OnTarget | Waiting. Waiting = chưa có ngày nào dùng được trong cửa sổ.</param>
public sealed record EnergyBalanceRelative(int Days, decimal? AvgKcal, int LoggedDays, string Status);

/// <summary>Insights &amp; Data của hai tab Energy Balance, tính đến hôm nay theo múi giờ user.</summary>
public sealed record EnergyBalanceInsightsResponse(
    DateOnly Today,
    IReadOnlyList<EnergyBalanceRelative> RelativeToExpenditure,
    IReadOnlyList<EnergyBalanceRelative> RelativeToTargets
);
