namespace Application;

/// <summary>Mọi goal của user, mới nhất trước (goal cũ giữ nguyên khi có goal mới). Dùng cho mục "Goal History" của tab Strategy.</summary>
public sealed record GetGoalHistoryQuery : IRequest<IReadOnlyList<GoalHistoryItemModel>>;
