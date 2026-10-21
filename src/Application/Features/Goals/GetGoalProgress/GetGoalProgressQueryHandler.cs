namespace Application;

public sealed class GetGoalProgressQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetGoalProgressQuery, GoalProgressResponse>
{
    public async Task<GoalProgressResponse> Handle(GetGoalProgressQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = FoodLogSupport.Today(_timeProvider);
        var goal = await GoalSupport.EnsureActiveGoalAsync(_uow, userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveGoal);

        var rows = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate >= goal.StartDate && w.LogDate <= today)
            .OrderBy(w => w.LogDate)
            .Select(w => new { w.LogDate, Kg = w.TrendWeightKg ?? w.WeightKg })
            .ToListAsync(ct);
        var points = rows.Select(r => (Date: r.LogDate, r.Kg)).ToList();

        decimal? current = points.Count > 0 ? points[^1].Kg : goal.StartWeightKg;
        var maintain = goal.Type == GoalType.Maintenance;

        decimal? progress = maintain ? null : GoalProgressCalculator.ProgressPercent(goal.StartWeightKg, goal.TargetWeightKg, current);
        decimal? deviation = maintain && current is { } c && goal.TargetWeightKg is { } t ? Math.Round(c - t, 2) : null;
        var eta = maintain ? null : GoalProgressCalculator.Eta(points, goal.TargetWeightKg, today);
        var weeks = GoalProgressCalculator.Weeks(goal.StartDate, goal.StartWeightKg, points, today);

        return new GoalProgressResponse(goal.Type.ToString(), goal.StartDate, goal.StartWeightKg, goal.TargetWeightKg, current, progress, deviation, eta, weeks);
    }
}
