namespace Application;

/// <param name="HasProposal">false khi expenditure đang Calibrating/Holding: preview không có đề xuất, Accept giữ nguyên target tuần trước.</param>
public sealed record CheckInContext(
    MacroProgram Program,
    MacroProgramWeek PrevWeek,
    ExpenditureSnapshot Tdee,
    decimal TrendNow,
    CheckInProposal Proposal,
    bool HasProposal,
    DateOnly? LastCheckIn,
    DateOnly PeriodStart,
    IReadOnlyList<CheckInDayModel> Days,
    CheckInPreviewResponse Preview
);

/// <summary>Gom dữ liệu + ALG-04 cho preview/session/accept/reject (UC-G02..G04).</summary>
public static class CheckInSupport
{
    private const string ModuleIntro = "intro", ModulePartialLogging = "partial-logging", ModuleProgramUpdate = "program-update";
    private const string Required = "required", Skippable = "skippable", Hidden = "hidden";

    public static async Task<CheckInContext> BuildAsync(IUnitOfWork uow, Guid userId, DateOnly today, AlgorithmOptions opt, CancellationToken ct, bool tracking = false)
    {
        var program = await uow.Repository<MacroProgram>().Query(tracking)
            .Include(p => p.Weeks)
            .Include(p => p.DayTargets)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);
        if (program.ProgramType == ProgramType.Manual)
            throw new BadRequestException(ExceptionMessages.ManualProgramNoCheckIn);

        var prev = program.Weeks.Where(w => w.EffectiveFrom <= today).OrderByDescending(w => w.EffectiveFrom).FirstOrDefault()
            ?? throw new BadRequestException(ExceptionMessages.ProgramHasNoWeekTarget);

        var tdee = await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate <= today)
            .OrderByDescending(s => s.SnapshotDate)
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException(ExceptionMessages.NoExpenditureEstimate);

        var weights = await uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate <= today && w.TrendWeightKg != null)
            .OrderByDescending(w => w.LogDate)
            .Select(w => new { w.LogDate, Trend = w.TrendWeightKg!.Value })
            .Take(60).ToListAsync(ct);
        var now = weights.FirstOrDefault() ?? throw new BadRequestException(ExceptionMessages.WeightRequired);
        var weekAgo = weights.FirstOrDefault(w => w.LogDate <= now.LogDate.AddDays(-7));
        decimal? rateActual = weekAgo is null ? null
            : -WeightTrendCalculator.RatePerWeek(weekAgo.LogDate, weekAgo.Trend, now.LogDate, now.Trend); // dương = giảm

        // 7 ngày gần nhất. Ngày user đánh dấu "log chưa đủ" không tính vào trung bình/số ngày log (expenditure cũng bỏ qua, xem ExpenditureRebuilder)
        var from7 = today.AddDays(-6);
        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, from7, today, ct);
        var valid = logs.Values.Where(d => !d.IsIncomplete).Select(d => d.TotalCalories).ToList();
        var markedIncomplete = logs.Values.Count(d => d.IsIncomplete);

        var tdeeByDate = (await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from7 && s.SnapshotDate <= today)
            .Select(s => new { s.SnapshotDate, s.EstimatedTdeeKcal })
            .ToListAsync(ct)).ToDictionary(s => s.SnapshotDate, s => s.EstimatedTdeeKcal);
        var days = Enumerable.Range(0, 7).Select(i => DayModel(program, logs, tdeeByDate, tdee.EstimatedTdeeKcal, from7.AddDays(i), opt)).ToList();

        var lastCheckIn = await uow.Repository<WeeklyCheckIn>().Query()
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CheckInDate)
            .Select(c => (DateOnly?)c.CheckInDate)
            .FirstOrDefaultAsync(ct);

        var proposal = CheckInCalculator.Propose(program, now.Trend, tdee.EstimatedTdeeKcal, prev, opt);
        var hasProposal = tdee.Status == ExpenditureStatus.Updating;
        var (next, due) = CheckInCalculator.Next(today, program.CheckInWeekday, program.StartDate, lastCheckIn);

        var summary = new CheckInWeekSummary(
            valid.Count == 0 ? 0 : Math.Round(valid.Average(), 0), valid.Count, markedIncomplete,
            weekAgo?.Trend, now.Trend, rateActual, proposal.RateTargetKg,
            tdee.EstimatedTdeeKcal, tdee.Status.ToString());

        var newTargets = hasProposal ? proposal.Targets : new MacroTargets(prev.CaloriesTarget, prev.ProteinTarget, prev.CarbsTarget, prev.FatTarget);
        var changed = hasProposal && (newTargets.Calories != prev.CaloriesTarget || newTargets.ProteinG != prev.ProteinTarget
            || newTargets.CarbsG != prev.CarbsTarget || newTargets.FatG != prev.FatTarget);

        var preview = new CheckInPreviewResponse(
            next, due, lastCheckIn == today, summary,
            hasProposal
                ? new CheckInProposalModel(
                    proposal.PreviousCalories, proposal.NewCalories, proposal.Delta,
                    proposal.Targets.ProteinG, proposal.Targets.CarbsG, proposal.Targets.FatG,
                    proposal.Clamped, proposal.FloorApplied,
                    CheckInCalculator.Reason(proposal, tdee.Status, rateActual, proposal.RateTargetKg),
                    program.ProgramType == ProgramType.Collaborative ? proposal.NewCalories * 7 : null)
                : null,
            changed,
            CheckInCalculator.ReasonCodes(proposal, tdee.Status, rateActual, proposal.RateTargetKg, markedIncomplete),
            new CheckInReasonParams(
                summary.AvgCaloriesIn, valid.Count, markedIncomplete, weekAgo is null ? null : Math.Round(now.Trend - weekAgo.Trend, 2),
                prev.CaloriesTarget, newTargets.Calories, prev.ProteinTarget, prev.CarbsTarget, prev.FatTarget,
                newTargets.ProteinG, newTargets.CarbsG, newTargets.FatG));

        return new CheckInContext(program, prev, tdee, now.Trend, proposal, hasProposal, lastCheckIn, from7, days, preview);
    }

    private static CheckInDayModel DayModel(
        MacroProgram program, IReadOnlyDictionary<DateOnly, FoodLogSupport.LoggedDay> logs, IReadOnlyDictionary<DateOnly, decimal> tdeeByDate,
        decimal latestTdee, DateOnly date, AlgorithmOptions opt)
    {
        logs.TryGetValue(date, out var log);
        var week = program.Weeks.Where(w => w.EffectiveFrom <= date).OrderByDescending(w => w.EffectiveFrom).FirstOrDefault();
        int? target = week is null ? null : ProgramTargetResolver.ForDate(week, program, date).CaloriesTarget;
        var tdee = tdeeByDate.GetValueOrDefault(date, latestTdee);
        var suspected = log is not null && log.TotalCalories > 0 && log.TotalCalories < opt.IncompleteDayKcalRatio * tdee;
        return new CheckInDayModel(date, log?.TotalCalories, target, suspected, log?.IsIncomplete ?? false);
    }

    /// <summary>Hub check-in: module + 7 ngày kcal + preview trong một lần đọc.</summary>
    public static async Task<CheckInSessionResponse> BuildSessionAsync(IUnitOfWork uow, Guid userId, DateOnly today, AlgorithmOptions opt, CancellationToken ct)
    {
        var c = await BuildAsync(uow, userId, today, opt, ct);
        var flags = await uow.Repository<UserProfile>().Query()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.CheckInIntroSeen, p.PartialLoggingModuleHidden })
            .FirstOrDefaultAsync(ct);

        var hasCandidates = c.Days.Any(d => d.SuspectedIncomplete || d.MarkedIncomplete);
        IReadOnlyList<CheckInModuleModel> modules =
        [
            new(ModuleIntro, c.LastCheckIn is null && flags?.CheckInIntroSeen != true ? Required : Hidden),
            new(ModulePartialLogging, flags?.PartialLoggingModuleHidden == true ? Hidden : hasCandidates ? Required : Skippable),
            new(ModuleProgramUpdate, Required),
        ];
        return new CheckInSessionResponse(c.Preview.NextCheckInDate, c.Preview.IsDue, c.Preview.AlreadyCheckedInToday, modules, c.Days, c.Preview);
    }

    /// <summary>
    /// Accept/Reject idempotent: hôm nay đã check-in cùng quyết định → trả lại Id cũ (không ghi mới, an toàn khi bấm đôi);
    /// quyết định khác hoặc chưa tới hạn → 409.
    /// </summary>
    public static async Task<Guid> DecideAsync(IUnitOfWork uow, Guid userId, DateOnly today, AlgorithmOptions opt, bool accepted, CancellationToken ct)
    {
        var existing = await uow.Repository<WeeklyCheckIn>().Query()
            .Where(c => c.UserId == userId && c.CheckInDate == today)
            .Select(c => new { c.Id, c.Accepted })
            .FirstOrDefaultAsync(ct);
        if (existing is not null)
            return existing.Accepted == accepted ? existing.Id : throw new ConflictException(ExceptionMessages.CheckInDecisionConflict);

        var c = await BuildAsync(uow, userId, today, opt, ct, tracking: true);
        if (!c.Preview.IsDue) throw new ConflictException(ExceptionMessages.CheckInNotDue(c.Preview.NextCheckInDate));
        return await CommitAsync(uow, c, today, accepted, ct);
    }

    /// <summary>
    /// Ghi MacroProgramWeek (hôm nay) + WeeklyCheckIn. Reject, hoặc Accept khi không có đề xuất: tuần mới copy target cũ (BR-17).
    /// Collaborative có target theo ngày: UpsertWeek chia lại 7 ngày theo ngân sách mới (ngày khoá giữ nguyên).
    /// </summary>
    public static async Task<Guid> CommitAsync(IUnitOfWork uow, CheckInContext c, DateOnly today, bool accepted, CancellationToken ct)
    {
        if (c.Preview.AlreadyCheckedInToday)
            throw new ConflictException(ExceptionMessages.CheckInAlreadyDone);

        var applied = accepted && c.HasProposal;
        var targets = applied
            ? c.Proposal.Targets
            : new MacroTargets(c.PrevWeek.CaloriesTarget, c.PrevWeek.ProteinTarget, c.PrevWeek.CarbsTarget, c.PrevWeek.FatTarget);
        ProgramWeekBuilder.UpsertWeek(c.Program, today, targets, applied ? AdjustmentReason.ExpenditureChange : AdjustmentReason.Manual);
        var week = c.Program.Weeks.First(w => w.EffectiveFrom == today);

        var checkIn = new WeeklyCheckIn
        {
            Id = Guid.NewGuid(),
            UserId = c.Program.UserId,
            CheckInDate = today,
            MacroProgramWeek = week, // qua navigation: week mới chưa có Id tới lúc lưu
            TdeeEstimate = c.Tdee.EstimatedTdeeKcal,
            TrendWeightKg = c.TrendNow,
            WeightChangeRateKgPerWeek = c.Preview.Week.RateActualKgPerWeek,
            PreviousCaloriesTarget = c.PrevWeek.CaloriesTarget,
            NewCaloriesTarget = week.CaloriesTarget,
            CaloriesDelta = week.CaloriesTarget - c.PrevWeek.CaloriesTarget,
            Accepted = accepted,
            WeeklyBudgetKcal = c.Program.ProgramType == ProgramType.Collaborative ? week.CaloriesTarget * 7 : null,
            PeriodStart = c.PeriodStart,
            PeriodEnd = today,
        };
        await uow.Repository<WeeklyCheckIn>().AddAsync(checkIn, ct);
        await uow.EnsureSaveAsync(ct);
        return checkIn.Id;
    }
}
