namespace Application;

public sealed record CheckInContext(
    MacroProgram Program,
    MacroProgramWeek PrevWeek,
    ExpenditureSnapshot Tdee,
    decimal TrendNow,
    CheckInProposal Proposal,
    CheckInPreviewResponse Preview
);

/// <summary>Gom dữ liệu + ALG-04 cho preview/accept/reject (UC-G02..G04).</summary>
public static class CheckInSupport
{
    public static async Task<CheckInContext> BuildAsync(IUnitOfWork uow, Guid userId, DateOnly today, AlgorithmOptions opt, CancellationToken ct, bool tracking = false)
    {
        var program = await uow.Repository<MacroProgram>().Query(tracking)
            .Include(p => p.Weeks)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct)
            ?? throw new NotFoundException("Chưa có program đang hoạt động.");
        if (program.ProgramType == ProgramType.Manual)
            throw new BadRequestException("Program Manual không có check-in.");

        var prev = program.Weeks.Where(w => w.EffectiveFrom <= today).OrderByDescending(w => w.EffectiveFrom).FirstOrDefault()
            ?? throw new BadRequestException("Program chưa có target tuần nào.");

        var tdee = await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate <= today)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException("Chưa có ước lượng expenditure.");

        var weights = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate <= today && w.TrendWeightKg != null)
            .OrderByDescending(w => w.LogDate)
            .Select(w => new { w.LogDate, Trend = w.TrendWeightKg!.Value })
            .Take(60).ToListAsync(ct);
        var now = weights.FirstOrDefault() ?? throw new BadRequestException("Cần ít nhất một lần cân nặng.");
        var weekAgo = weights.FirstOrDefault(w => w.LogDate <= now.LogDate.AddDays(-7));
        decimal? rateActual = weekAgo is null ? null
            : -WeightTrendCalculator.RatePerWeek(weekAgo.LogDate, weekAgo.Trend, now.LogDate, now.Trend); // dương = giảm

        var from7 = today.AddDays(-6);
        var logs = (await FoodLogSupport.LoadDaysAsync(uow, userId, from7, today, ct))
            .Values.Where(d => d.IsLogged).Select(d => d.TotalCalories).ToList();

        var lastCheckIn = await uow.Repository<WeeklyCheckIn>().Query()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CheckInDate)
            .Select(c => (DateOnly?)c.CheckInDate)
            .FirstOrDefaultAsync(ct);

        var proposal = CheckInCalculator.Propose(program, now.Trend, tdee.EstimatedTdeeKcal, prev, opt);
        var (next, due) = CheckInCalculator.Next(today, program.CheckInWeekday, program.StartDate, lastCheckIn);

        var preview = new CheckInPreviewResponse(
            next, due, lastCheckIn == today,
            new CheckInWeekSummary(
                logs.Count == 0 ? 0 : Math.Round(logs.Average(), 0), logs.Count,
                weekAgo?.Trend, now.Trend, rateActual, proposal.RateTargetKg,
                tdee.EstimatedTdeeKcal, tdee.Status.ToString()),
            new CheckInProposalModel(
                proposal.PreviousCalories, proposal.NewCalories, proposal.Delta,
                proposal.Targets.ProteinG, proposal.Targets.CarbsG, proposal.Targets.FatG,
                proposal.Clamped, proposal.FloorApplied,
                CheckInCalculator.Reason(proposal, tdee.Status, rateActual, proposal.RateTargetKg),
                program.ProgramType == ProgramType.Collaborative ? proposal.NewCalories * 7 : null));

        return new CheckInContext(program, prev, tdee, now.Trend, proposal, preview);
    }

    /// <summary>Ghi MacroProgramWeek (hôm nay) + WeeklyCheckIn. Reject: tuần mới copy target cũ (ponytail: entity đòi MacroProgramWeekId, BR-17 giữ target).</summary>
    public static async Task<Guid> CommitAsync(IUnitOfWork uow, CheckInContext c, DateOnly today, bool accepted, CancellationToken ct)
    {
        if (c.Preview.AlreadyCheckedInToday)
            throw new ConflictException("Hôm nay đã check-in.");

        var targets = accepted
            ? c.Proposal.Targets
            : new MacroTargets(c.PrevWeek.CaloriesTarget, c.PrevWeek.ProteinTarget, c.PrevWeek.CarbsTarget, c.PrevWeek.FatTarget);
        ProgramWeekBuilder.UpsertWeek(c.Program, today, targets, accepted ? AdjustmentReason.ExpenditureChange : AdjustmentReason.Manual);
        var week = c.Program.Weeks.First(w => w.EffectiveFrom == today);

        var checkIn = new WeeklyCheckIn
        {
            Id = Guid.NewGuid(),
            UserId = c.Program.UserId,
            CheckInDate = today,
            MacroProgramWeekId = week.Id,
            TdeeEstimate = c.Tdee.EstimatedTdeeKcal,
            TrendWeightKg = c.TrendNow,
            WeightChangeRateKgPerWeek = c.Preview.Week.RateActualKgPerWeek,
            PreviousCaloriesTarget = c.PrevWeek.CaloriesTarget,
            NewCaloriesTarget = targets.Calories,
            CaloriesDelta = targets.Calories - c.PrevWeek.CaloriesTarget,
            Accepted = accepted,
            WeeklyBudgetKcal = c.Program.ProgramType == ProgramType.Collaborative ? targets.Calories * 7 : null,
        };
        await uow.Repository<WeeklyCheckIn>().AddAsync(checkIn, ct);
        await uow.EnsureSaveAsync(ct);
        return checkIn.Id;
    }
}
