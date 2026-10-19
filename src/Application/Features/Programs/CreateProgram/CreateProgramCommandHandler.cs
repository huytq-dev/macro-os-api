namespace Application;

public sealed class CreateProgramCommandHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider
) : IRequestHandler<CreateProgramCommand, Guid>
{
    public async Task<Guid> Handle(CreateProgramCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        var profile = await _uow.Repository<UserProfile>().FindTrackedAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        if (profile.DateOfBirth is null || profile.HeightCm is null)
            throw new BadRequestException(ExceptionMessages.ProfileRequiredForProgram);

        var latestWeight = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException(ExceptionMessages.WeightRequiredForProgram);

        // Chỉ một program active (BR-01): kết thúc program cũ, giữ lịch sử
        var activePrograms = await _uow.Repository<MacroProgram>().GetAllTrackedAsync(p => p.UserId == userId && p.IsActive, ct);
        foreach (var old in activePrograms) { old.IsActive = false; old.Status = NutritionProgramStatus.Superseded; }

        var program = ProgramDraftCalculator.BuildProgram(request, profile, latestWeight, today);

        if (request.TrainingType is { } training) profile.TrainingType = training;
        // Cùng công thức với bước xem trước của wizard (có tỉ lệ mỡ thì dùng Katch-McArdle) để số liệu khớp
        var tdee = request.ManualInitialTdeeKcal ?? OnboardingCalculator.EstimateTdee(profile, latestWeight, today).TdeeKcal;

        if (request.DayTargets is { Count: > 0 })
        {
            // User tự đặt từng ngày (Collaborative/Manual): lưu 7 dòng; target tuần = trung bình 7 ngày để chỗ chỉ biết target tuần vẫn đúng
            var days = ProgramDraftCalculator.Days(request, program, latestWeight, tdee, _options.Value);
            program.DayTargets = ProgramDayTargets.FromInputs(program.Id, days);
            ProgramWeekBuilder.UpsertWeek(program, today, ProgramDayTargets.Average(program.DayTargets), AdjustmentReason.Initial);
        }
        else
        {
            if (program.ProgramType == ProgramType.Manual) throw new BadRequestException(ExceptionMessages.ManualProgramNeedsSevenDays);
            var targets = ProgramWeekBuilder.Compute(program, latestWeight, tdee, _options.Value);
            ProgramWeekBuilder.UpsertWeek(program, today, targets, AdjustmentReason.Initial);
        }

        // Goal là nguồn cân nặng mục tiêu: đóng goal cũ, tạo goal mới gắn với program (UC-B08)
        foreach (var previous in await _uow.Repository<Goal>().GetAllTrackedAsync(g => g.UserId == userId && g.Status == GoalStatus.Active, ct))
        {
            previous.Status = GoalStatus.Superseded;
            previous.EndDate = today;
        }
        var goal = new Goal
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Type = request.GoalType,
            TargetWeightKg = request.TargetWeightKg,
            StartWeightKg = latestWeight,
            RatePerWeekKg = program.WeeklyWeightChangeTargetKg,
            StartDate = today,
            Status = GoalStatus.Active,
        };
        await _uow.Repository<Goal>().AddAsync(goal, ct);
        program.GoalId = goal.Id;

        await _uow.Repository<MacroProgram>().AddAsync(program, ct);

        // Snapshot expenditure đầu: Calibrating với TDEE₀ (ST-01)
        var snapshot = await _uow.Repository<ExpenditureSnapshot>().FindTrackedAsync(s => s.UserId == userId && s.SnapshotDate == today, ct);
        if (snapshot is null)
        {
            snapshot = new ExpenditureSnapshot { Id = Guid.NewGuid(), UserId = userId, SnapshotDate = today };
            await _uow.Repository<ExpenditureSnapshot>().AddAsync(snapshot, ct);
        }
        snapshot.EstimatedTdeeKcal = tdee;
        snapshot.Status = ExpenditureStatus.Calibrating;
        snapshot.AvgCaloriesConsumed = 0;
        snapshot.WeightChangeRateKgPerWeek = null;
        snapshot.WindowDays = 0;

        await _uow.EnsureSaveAsync(ct);
        return program.Id;
    }
}
