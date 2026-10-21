namespace Application;

/// <summary>UC-G03: chấp nhận → MacroProgramWeek mới từ hôm nay + WeeklyCheckIn(Accepted).</summary>
public sealed record AcceptCheckInCommand : IRequest<Guid>;
