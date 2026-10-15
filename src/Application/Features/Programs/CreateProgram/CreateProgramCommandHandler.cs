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

        var profile = await _uow.Repository<UserProfile>().FindAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        if (profile.DateOfBirth is null || profile.HeightCm is null)
            throw new BadRequestException("Cần ngày sinh và chiều cao trong hồ sơ trước khi tạo program.");

        var latestWeight = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException("Cần ít nhất một lần cân nặng trước khi tạo program.");

        // Chỉ một program active (BR-01): kết thúc program cũ, giữ lịch sử
        var activePrograms = await _uow.Repository<MacroProgram>().GetAllTrackedAsync(p => p.UserId == userId && p.IsActive, ct);
        foreach (var old in activePrograms) old.IsActive = false;

        var ratePercent = request.GoalType switch
        {
            GoalType.Maintenance => 0,
            GoalType.WeightGain  => -Math.Abs(request.WeeklyWeightChangeTargetPercent),
            _                    => Math.Abs(request.WeeklyWeightChangeTargetPercent),
        };

        var program = new MacroProgram
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ProgramType = request.ProgramType,
            GoalType = request.GoalType,
            DietStyle = request.DietStyle,
            WeeklyWeightChangeTargetPercent = ratePercent,
            WeeklyWeightChangeTargetKg = Math.Round(-ratePercent / 100m * latestWeight, 2),
            ProteinLevel = request.ProteinLevel,
            ProteinGPerKgBodyweight = MacroTargetCalculator.ProteinGPerKg(request.ProteinLevel, profile.TrainingType),
            CalorieFloorKcal = request.CalorieFloorKcal,
            CheckInWeekday = request.CheckInWeekday ?? today.DayOfWeek,
            StartDate = today,
            ExpenditureStartDate = today,
            ManualInitialTdeeKcal = request.ManualInitialTdeeKcal,
        };

        var tdee = request.ManualInitialTdeeKcal ?? MacroTargetCalculator.InitialTdee(
            profile.Gender, MacroTargetCalculator.AgeYears(profile.DateOfBirth.Value, today),
            profile.HeightCm.Value, latestWeight, profile.ActivityLevel);

        if (program.ProgramType != ProgramType.Manual)
        {
            var targets = ProgramWeekBuilder.Compute(program, latestWeight, tdee, _options.Value);
            ProgramWeekBuilder.UpsertWeek(program, today, targets, AdjustmentReason.Initial);
        }

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
