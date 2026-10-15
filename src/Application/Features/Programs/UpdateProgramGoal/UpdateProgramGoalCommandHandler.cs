namespace Application;

public sealed class UpdateProgramGoalCommandHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider
) : IRequestHandler<UpdateProgramGoalCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProgramGoalCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        var program = await _uow.Repository<MacroProgram>().Query(tracking: true)
            .Include(p => p.Weeks)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct)
            ?? throw new NotFoundException("Chưa có program đang hoạt động.");

        var profile = await _uow.Repository<UserProfile>().FindAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        if (request.GoalType.HasValue) program.GoalType = request.GoalType.Value;
        if (request.DietStyle.HasValue) program.DietStyle = request.DietStyle.Value;
        if (request.CheckInWeekday.HasValue) program.CheckInWeekday = request.CheckInWeekday.Value;
        if (request.CalorieFloorKcal.HasValue) program.CalorieFloorKcal = request.CalorieFloorKcal;
        if (request.FixedProteinGrams.HasValue) program.FixedProteinGrams = request.FixedProteinGrams;
        if (request.ProteinLevel.HasValue)
        {
            program.ProteinLevel = request.ProteinLevel.Value;
            program.ProteinGPerKgBodyweight = MacroTargetCalculator.ProteinGPerKg(program.ProteinLevel, profile.TrainingType);
        }

        var latestWeight = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException("Cần ít nhất một lần cân nặng.");

        if (request.WeeklyWeightChangeTargetPercent.HasValue || request.GoalType.HasValue)
        {
            var raw = request.WeeklyWeightChangeTargetPercent ?? Math.Abs(program.WeeklyWeightChangeTargetPercent);
            program.WeeklyWeightChangeTargetPercent = program.GoalType switch
            {
                GoalType.Maintenance => 0,
                GoalType.WeightGain  => -Math.Abs(raw),
                _                    => Math.Abs(raw),
            };
            program.WeeklyWeightChangeTargetKg = Math.Round(-program.WeeklyWeightChangeTargetPercent / 100m * latestWeight, 2);
        }

        if (program.ProgramType != ProgramType.Manual)
        {
            var tdee = await _uow.Repository<ExpenditureSnapshot>().Query()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SnapshotDate)
                .Select(s => (int?)s.EstimatedTdeeKcal)
                .FirstOrDefaultAsync(ct)
                ?? program.ManualInitialTdeeKcal
                ?? throw new BadRequestException("Chưa có ước lượng expenditure.");

            var targets = ProgramWeekBuilder.Compute(program, latestWeight, tdee, _options.Value);
            ProgramWeekBuilder.UpsertWeek(program, today, targets, AdjustmentReason.Manual);
        }

        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
