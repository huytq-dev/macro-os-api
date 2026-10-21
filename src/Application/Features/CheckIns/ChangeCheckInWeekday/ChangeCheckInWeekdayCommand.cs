namespace Application;

/// <summary>UC-G06: đổi ngày check-in trong tuần; không sinh lại target.</summary>
public sealed record ChangeCheckInWeekdayCommand(DayOfWeek CheckInWeekday) : IRequest<Unit>;
