namespace Application;

public sealed class SetManualExpenditureCommandHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider
) : IRequestHandler<SetManualExpenditureCommand, Unit>
{
    public async Task<Unit> Handle(SetManualExpenditureCommand request, CancellationToken ct)
    {
        if (request.TdeeKcal is < 1000 or > 6000)
            throw new BadRequestException(ExceptionMessages.Invalid("TdeeKcal"));

        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;   // ngày local của user

        var program = await _uow.Repository<MacroProgram>().Query(tracking: true)
            .Include(p => p.Weeks)
            .Include(p => p.DayTargets)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);

        program.ManualInitialTdeeKcal = request.TdeeKcal;
        program.ExpenditureStartDate = today;

        var snapshot = await _uow.Repository<ExpenditureSnapshot>().FindTrackedAsync(s => s.UserId == userId && s.SnapshotDate == today, ct);
        if (snapshot is null)
        {
            snapshot = new ExpenditureSnapshot { Id = Guid.NewGuid(), UserId = userId, SnapshotDate = today };
            await _uow.Repository<ExpenditureSnapshot>().AddAsync(snapshot, ct);
        }
        snapshot.EstimatedTdeeKcal = request.TdeeKcal;
        snapshot.Status = ExpenditureStatus.Calibrating;
        snapshot.AlgorithmVersion = AlgorithmVersions.ExpenditureFor(_options.Value);
        snapshot.AvgCaloriesConsumed = 0;
        snapshot.WeightChangeRateKgPerWeek = null;
        snapshot.WindowDays = 0;

        if (program.ProgramType != ProgramType.Manual)
        {
            var latestWeight = await _uow.Repository<WeightEntry>().Query()
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.LogDate)
                .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
                .FirstOrDefaultAsync(ct)
                ?? throw new BadRequestException(ExceptionMessages.WeightRequired);

            var targets = ProgramWeekBuilder.Compute(program, latestWeight, request.TdeeKcal, _options.Value);
            ProgramWeekBuilder.UpsertWeek(program, today, targets, AdjustmentReason.Manual);
        }

        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
