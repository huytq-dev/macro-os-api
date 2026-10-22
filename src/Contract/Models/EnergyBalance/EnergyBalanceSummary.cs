namespace Contract;

/// <summary>
/// Thống kê khoảng. Mọi số trung bình là TRUNG BÌNH MỖI NGÀY ĐÃ LOG trong khoảng (ngày có CaloriesKcal, không IsIncomplete, kể cả nhịn ăn = 0 kcal), không chia cho độ dài khoảng.
/// Trung bình chênh lệch chỉ tính các ngày có cả calories lẫn expenditure (hoặc target).
/// </summary>
/// <param name="LoggedDays">Số ngày đã log hợp lệ trong khoảng.</param>
/// <param name="AvgCaloriesKcal">Trung bình kcal của MỌI ngày hợp lệ (giữ để tương thích). Để vẽ thẻ từng tab dùng AvgCaloriesVsExpenditureKcal / AvgCaloriesVsTargetKcal.</param>
/// <param name="AvgCaloriesVsExpenditureKcal">Tab Expenditure: trung bình kcal CHỈ trên các ngày có cả kcal và expenditure (cùng tập với AvgExpenditureKcal và AvgVsExpenditureKcal): AvgCaloriesVsExpenditureKcal − AvgExpenditureKcal = AvgVsExpenditureKcal.</param>
/// <param name="AvgCaloriesVsTargetKcal">Tab Calorie Targets: trung bình kcal CHỈ trên các ngày có cả kcal và target (cùng tập với AvgTargetKcal và AvgVsTargetKcal): AvgCaloriesVsTargetKcal − AvgTargetKcal = AvgVsTargetKcal.</param>
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
    string VsTargetStatus,
    decimal? AvgCaloriesVsExpenditureKcal = null,
    decimal? AvgCaloriesVsTargetKcal = null,
    int UnverifiedTargetDays = 0
);
// UnverifiedTargetDays: số ngày hợp lệ có so với target nhưng target LEGACY_UNVERIFIED: bị LOẠI khỏi AvgTargetKcal/AvgVsTargetKcal/VsTargetDays/VsTargetStatus/AvgCaloriesVsTargetKcal (cùng cho bucket và relative); FE hiện "Historical target not verified" khi > 0.
