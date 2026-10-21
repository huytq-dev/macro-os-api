namespace Contract;

/// <summary>7 ngày Thứ 2 → Chủ nhật của tuần được hỏi.</summary>
public sealed record WeekLogResponse(IReadOnlyList<WeekDayModel> Days);
