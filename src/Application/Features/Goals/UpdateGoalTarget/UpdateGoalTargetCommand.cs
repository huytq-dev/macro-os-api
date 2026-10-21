namespace Application;

/// <summary>UC-B08: đặt/xoá cân nặng mục tiêu của goal đang hoạt động (null = xoá target).</summary>
public sealed record UpdateGoalTargetCommand(decimal? TargetWeightKg) : IRequest<Unit>;
