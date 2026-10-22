namespace Application;

/// <summary>
/// Expenditure ĐANG HIỆU LỰC của một ngày (Static/Dynamic, S25): <c>Static ? giá trị Static : TDEE đã học</c> (thiết kế Expenditure_Settings_Final_Design.md §6.1). Hàm thuần, CHƯA nối vào luồng nào
/// (series, Energy Balance, check-in, Create đều còn dùng TDEE đã học): chỉ nối sau khi S25 được áp và được duyệt. Không bao giờ sửa snapshot đã học: Static chỉ là lớp phủ theo ngày nên Static → Dynamic giữ nguyên lịch sử học.
/// Ngày là ngày LOCAL của user (xem <see cref="ExpenditureModeTimeline"/>).
/// </summary>
public static class ActiveExpenditureResolver
{
    public const string Learned = "Learned", StaticOverride = "Static";

    public static (decimal TdeeKcal, string Source) Resolve(IEnumerable<ExpenditureModePeriod> periods, DateOnly date, decimal learnedTdeeKcal)
    {
        var (mode, staticTdee) = ExpenditureModeTimeline.ModeAt(periods, date);
        return mode == ExpenditureMode.Static && staticTdee is > 0 ? (staticTdee.Value, StaticOverride) : (learnedTdeeKcal, Learned);
    }

    /// <summary>Check-in không đề xuất thay đổi target trong Static (TDEE do user đặt, không học): true khi ngày đó đang Static.</summary>
    public static bool IsStatic(IEnumerable<ExpenditureModePeriod> periods, DateOnly date) => ExpenditureModeTimeline.ModeAt(periods, date).Mode == ExpenditureMode.Static;
}
