namespace Application;

/// <summary>UC-G04 (BR-17): từ chối → giữ target cũ, ghi WeeklyCheckIn(Accepted = false).</summary>
public sealed record RejectCheckInCommand : IRequest<Guid>;
