namespace Application;

/// <summary>Hub check-in kiểu MacroFactor: module + 7 ngày kcal (kèm ngày nghi chưa log đủ) + preview, một request.</summary>
public sealed record GetCheckInSessionQuery : IRequest<CheckInSessionResponse>;
