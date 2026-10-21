namespace Application;

/// <summary>UC-H04/F06: tiến độ goal — % hoàn thành (lose/gain) hoặc độ lệch (maintenance), waterfall theo tuần từ ngày bắt đầu, ngày dự kiến.</summary>
public sealed record GetGoalProgressQuery : IRequest<GoalProgressResponse>;
