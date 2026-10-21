namespace Contract;

/// <param name="Target">Target của MacroProgramWeek hiệu lực tại ngày này; null nếu chưa có program.</param>
/// <param name="IsLogged">Ngày có ≥ 1 món chưa xoá; ngày không có món = false.</param>
public sealed record WeekDayModel(DateOnly LogDate, bool IsLogged, MacroTotals Totals, ProgramWeekModel? Target);
