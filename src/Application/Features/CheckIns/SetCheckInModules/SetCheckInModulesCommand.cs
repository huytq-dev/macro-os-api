namespace Application;

/// <summary>Cờ module check-in của user: đã xem Introduction; tắt/bật lại Partial Logging (More → Coaching Modules). Trường null = giữ nguyên.</summary>
public sealed record SetCheckInModulesCommand(bool? IntroSeen, bool? PartialLoggingHidden) : IRequest<Unit>;
