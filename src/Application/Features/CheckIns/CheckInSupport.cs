namespace Application;

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
        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, from7, today, ct, includeFasting: true);
        var valid = logs.Values.Select(d => d.AlgorithmKcal).Where(k => k.HasValue).Select(k => k!.Value).ToList();
        var markedIncomplete = logs.Values.Count(d => d.IsIncomplete);

        var tdeeByDate = (await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from7 && s.SnapshotDate <= today)
            .Select(s => new { s.SnapshotDate, s.EstimatedTdeeKcal })
            .ToListAsync(ct)).ToDictionary(s => s.SnapshotDate, s => s.EstimatedTdeeKcal);
        // Target từng ngày của 7 ngày xem lại: tuần hiệu lực ngày đó, kể cả program trước đó nếu program active mới bắt đầu giữa chừng; đề xuất tuần mới vẫn tính trên program active
        var history = await ProgramHistory.LoadAsync(uow, userId, from7, today, ct);
        var days = Enumerable.Range(0, 7).Select(i => DayModel(history, logs, tdeeByDate, tdee.EstimatedTdeeKcal, from7.AddDays(i), opt, today)).ToList();

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

        // Kiểm khả thi của đề xuất (kcal > 0, macro ≤ kcal, shift, 7 ngày) TRƯỚC khi user thấy nút Accept: không khả thi thì KHÔNG bao giờ tự áp dụng; Accept sẽ bị từ chối 400 và giữ target đã chấp nhận
        var feasibility = hasProposal ? ProgramWeekBuilder.Assess(program, proposal.Targets) : TargetFeasibility.Feasible();
        var direction = GoalRules.DirectionOf(program.GoalType);
        var reasonCodes = CheckInCalculator.ReasonCodes(proposal, tdee.Status, rateActual, proposal.RateTargetKg, markedIncomplete, direction).ToList();
        if (!feasibility.IsFeasible) reasonCodes.Add(CheckInCalculator.InfeasibleProposal);

        // Đề xuất dùng TDEE của snapshot: báo FE dữ liệu dẫn xuất đã mới chưa; Accept bị chặn khi chưa Ready (xem DecideAsync)
        var derivedState = await DerivedDataJobs.OpenStateAsync(uow, userId, opt, ct);

        var preview = new CheckInPreviewResponse(
            next, due, lastCheckIn == today, summary,
            hasProposal
                ? new CheckInProposalModel(
                    proposal.PreviousCalories, proposal.NewCalories, proposal.Delta,
                    proposal.Targets.ProteinG, proposal.Targets.CarbsG, proposal.Targets.FatG,
                    proposal.Clamped, proposal.FloorApplied,
                    CheckInCalculator.Reason(proposal, tdee.Status, rateActual, proposal.RateTargetKg, direction),
                    program.ProgramType == ProgramType.Collaborative ? proposal.NewCalories * 7 : null)
                : null,
            changed,
            reasonCodes,
            new CheckInReasonParams(
                summary.AvgCaloriesIn, valid.Count, markedIncomplete, weekAgo is null ? null : Math.Round(now.Trend - weekAgo.Trend, 2),
                prev.CaloriesTarget, newTargets.Calories, prev.ProteinTarget, prev.CarbsTarget, prev.FatTarget,
                newTargets.ProteinG, newTargets.CarbsG, newTargets.FatG),
            feasibility,
            derivedState,
            CheckInGate.AcceptBlockReason(derivedState));

        return new CheckInContext(program, prev, tdee, now.Trend, proposal, hasProposal, lastCheckIn, from7, days, preview);
    }

    private static CheckInDayModel DayModel(
        IReadOnlyList<MacroProgram> programs, IReadOnlyDictionary<DateOnly, FoodLogSupport.LoggedDay> logs, IReadOnlyDictionary<DateOnly, decimal> tdeeByDate,
        decimal latestTdee, DateOnly date, AlgorithmOptions opt, DateOnly today)
    {
        logs.TryGetValue(date, out var log);
        var targetModel = ProgramHistory.TargetOn(programs, date, today);
        int? target = targetModel?.CaloriesTarget;
        var tdee = tdeeByDate.GetValueOrDefault(date, latestTdee);
        // Calories HIỆU LỰC của ngày (manual thắng Food Log); null = chưa biết (override macro-only), không phải 0
        var suspected = log is not null && log.Calories > 0 && log.Calories < opt.IncompleteDayKcalRatio * tdee;
        return new CheckInDayModel(date, log?.Calories, target, suspected, log?.IsIncomplete ?? false, targetModel?.TargetProvenance);
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
        if (await ExistingDecisionAsync(uow, userId, today, accepted, ct) is { } done) return done;   // đường nhanh, ngoài khoá

        try
        {
            return await DecideCoreAsync(uow, userId, today, opt, accepted, ct);
        }
        catch (DbUpdateException)
        {
            // Hai quyết định ghi chồng nhau (Reject không lấy khoá; hoặc Accept và Reject ở hai tab): bên thua vi phạm unique (tuần mới cùng ngày / check-in cùng ngày)
            // và MỌI ghi của nó đã rollback (transaction của SaveChanges hoặc của khoá) ⇒ trả kết quả idempotent theo quyết định đã thắng thay vì 500
            uow.DiscardChanges();
            if (await ExistingDecisionAsync(uow, userId, today, accepted, ct) is { } winner) return winner;
            throw;
        }
        catch (ConflictException blocked) when (blocked.Errors is { } errors && errors.TryGetValue("reasonCode", out var codes) && codes.Contains(CheckInGate.Updating))
        {
            // Accept chờ khoá quá DerivedLockWaitMs (một lần dựng hoặc một Accept ở tab khác đang giữ khoá, DB xa) hoặc bị cổng chặn: nếu trong lúc đó tab khác đã ghi quyết định hôm nay
            // thì trả kết quả idempotent (Id, hoặc 409 CheckInDecisionConflict) thay vì 409 DERIVED_DATA_UPDATING gây hiểu lầm; chưa có quyết định ⇒ giữ nguyên 409
            if (await ExistingDecisionAsync(uow, userId, today, accepted, ct) is { } decided) return decided;
            throw;
        }
    }

    private static async Task<Guid> DecideCoreAsync(IUnitOfWork uow, Guid userId, DateOnly today, AlgorithmOptions opt, bool accepted, CancellationToken ct)
    {
        // Reject chỉ chép target đã chấp nhận (không dùng TDEE): luôn được phép
        if (!accepted) return await BuildAndCommitAsync(uow, userId, today, opt, accepted, ct);

        // Accept dùng TDEE trong snapshot: chạy dưới CÙNG khoá user với dựng lại (không để một lần dựng xen giữa lúc đọc trạng thái và ghi tuần mới),
        // và từ chối 409 (không ghi gì) khi dữ liệu dẫn xuất chưa Ready (G6). Khoá bận quá DerivedLockWaitMs = một lần dựng lại đang chạy ⇒ cũng 409 DERIVED_DATA_UPDATING.
        // Thứ tự: BEGIN TRAN → sp_getapplock (cùng khoá với mọi lần dựng) → ĐỌC LẠI quyết định hôm nay → ĐỌC trạng thái job → dựng context (TDEE, trend, target) → ghi tuần + check-in → COMMIT (nhả khoá).
        return await DerivedDataLock.RunAsync(uow, userId, opt, async () =>
        {
            // Đọc lại DƯỚI khoá: một Accept song song (hai tab/thiết bị) có thể đã ghi quyết định trong lúc request này chờ khoá ⇒ trả Id đó (idempotent), không 409 CheckInNotDue/DERIVED_DATA_UPDATING
            if (await ExistingDecisionAsync(uow, userId, today, accepted, ct) is { } decided) return decided;
            var state = await DerivedDataJobs.OpenStateAsync(uow, userId, opt, ct);
            if (CheckInGate.AcceptBlockReason(state) is { } reason)
                throw new ConflictException(ExceptionMessages.CheckInDerivedDataNotReady, CheckInGate.Errors(reason));
            return await BuildAndCommitAsync(uow, userId, today, opt, accepted, ct);
        }, ct, ExceptionMessages.CheckInDerivedDataNotReady, CheckInGate.Errors(CheckInGate.Updating));
    }

    /// <summary>Quyết định đã ghi hôm nay: cùng quyết định ⇒ Id cũ (bấm đôi/hai tab an toàn); quyết định khác ⇒ 409 CheckInDecisionConflict; chưa có ⇒ null.</summary>
    private static async Task<Guid?> ExistingDecisionAsync(IUnitOfWork uow, Guid userId, DateOnly today, bool accepted, CancellationToken ct)
    {
        var existing = await uow.Repository<WeeklyCheckIn>().Query()
            .Where(c => c.UserId == userId && c.CheckInDate == today)
            .Select(c => new { c.Id, c.Accepted })
            .FirstOrDefaultAsync(ct);
        if (existing is null) return null;
        return existing.Accepted == accepted ? existing.Id : throw new ConflictException(ExceptionMessages.CheckInDecisionConflict);
    }

    private static async Task<Guid> BuildAndCommitAsync(IUnitOfWork uow, Guid userId, DateOnly today, AlgorithmOptions opt, bool accepted, CancellationToken ct)
    {
        var c = await BuildAsync(uow, userId, today, opt, ct, tracking: true);
        // Một quyết định song song vừa được ghi sau đường nhanh (hai tab): trả kết quả idempotent (Id, hoặc 409 CheckInDecisionConflict), không phải 409 CheckInNotDue
        if (c.Preview.AlreadyCheckedInToday)
            return await ExistingDecisionAsync(uow, userId, today, accepted, ct) ?? throw new ConflictException(ExceptionMessages.CheckInAlreadyDone);
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
        // Accept một đề xuất mới phải khả thi (400 + FeasibilityResponse, KHÔNG ghi gì, giữ target đã chấp nhận); Reject/Accept-không-đề-xuất chỉ sao chép target đã chấp nhận trước đó nên không kiểm lại
        ProgramWeekBuilder.UpsertWeek(c.Program, today, targets, applied ? AdjustmentReason.ExpenditureChange : AdjustmentReason.Manual, validate: applied);
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
