namespace Application;

/// <summary>Nạp trạng thái hiện tại cho Edit Goal (xem trước và áp dụng dùng chung).</summary>
public static class GoalEditSupport
{
    /// <param name="tracking">true khi sắp sửa Goal/MacroProgram (áp dụng); false cho xem trước (không ghi gì).</param>
    /// <returns>Goal null nếu program tạo trước khi có bảng Goal; khi áp dụng dùng <see cref="GoalSupport.EnsureActiveGoalAsync"/> để dựng.</returns>
    public static async Task<(GoalEditState State, Goal? Goal, MacroProgram Program)> LoadAsync(IUnitOfWork uow, Guid userId, bool tracking, CancellationToken ct)
    {
        var program = await uow.Repository<MacroProgram>().Query(tracking)
            .Include(p => p.Weeks)
            .Include(p => p.DayTargets)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);

        var goal = await uow.Repository<Goal>().Query(tracking)
            .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
            .OrderByDescending(g => g.StartDate)
            .FirstOrDefaultAsync(ct);

        var weight = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException(ExceptionMessages.WeightRequired);

        var snapshotTdee = await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => (decimal?)s.EstimatedTdeeKcal)
            .FirstOrDefaultAsync(ct);
        int? tdee = snapshotTdee is { } d ? (int)Math.Round(d) : program.ManualInitialTdeeKcal;

        var state = new GoalEditState(
            weight, goal?.Type ?? program.GoalType, goal is null ? program.TargetWeightKg : goal.TargetWeightKg, goal?.StartDate ?? program.StartDate,
            program.ProgramType, program.WeeklyWeightChangeTargetPercent, program.CalorieFloorKcal, tdee);
        return (state, goal, program);
    }
}
