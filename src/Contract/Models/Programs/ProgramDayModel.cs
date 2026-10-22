namespace Contract;

/// <summary>Target hiệu lực của một ngày trong tuần (đã tính DayTarget hoặc dịch kcal); Thứ 2 → Chủ nhật. <c>TargetProvenance</c>: VERIFIED | LEGACY_UNVERIFIED (xem <see cref="ProgramWeekModel"/>).</summary>
public sealed record ProgramDayModel(string DayOfWeek, int Calories, decimal ProteinG, decimal CarbsG, decimal FatG, bool IsLocked, string? TargetProvenance = null);
