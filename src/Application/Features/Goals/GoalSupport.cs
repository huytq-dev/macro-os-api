namespace Application;

public static class GoalSupport
{
    /// <summary>
    /// Goal đang Active của user (tracking). Chưa có (program tạo trước S9) thì dựng từ program đang hoạt động rồi lưu; không có program → null.
    /// Goal là nguồn cân nặng mục tiêu; MacroProgram.TargetWeightKg chỉ đồng bộ lúc tạo.
    /// </summary>
    public static async Task<Goal?> EnsureActiveGoalAsync(IUnitOfWork uow, Guid userId, CancellationToken ct)
    {
        var goal = await uow.Repository<Goal>().Query(tracking: true)
            .Where(g => g.UserId == userId && g.Status == GoalStatus.Active)
            .OrderByDescending(g => g.StartDate)
            .FirstOrDefaultAsync(ct);
        if (goal is not null) return goal;

        var program = await uow.Repository<MacroProgram>().Query(tracking: true)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct);
        if (program is null) return null;

        var startWeight = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate <= program.StartDate)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct);

        goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = program.GoalType,
            TargetWeightKg = program.TargetWeightKg,
            StartWeightKg = startWeight,
            RatePerWeekKg = program.WeeklyWeightChangeTargetKg,
            StartDate = program.StartDate,
            Status = GoalStatus.Active,
        };
        await uow.Repository<Goal>().AddAsync(goal, ct);
        program.GoalId = goal.Id;
        await uow.SaveChangesAsync(ct);
        return goal;
    }
}
