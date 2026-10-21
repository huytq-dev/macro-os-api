namespace Application;

public sealed class ResetExpenditureCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<ResetExpenditureCommand, Unit>
{
    public async Task<Unit> Handle(ResetExpenditureCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = FoodLogSupport.Today(_timeProvider);

        var program = await _uow.Repository<MacroProgram>().FindTrackedAsync(p => p.UserId == userId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);

        var latestTdee = await _uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => (decimal?)s.EstimatedTdeeKcal)
            .FirstOrDefaultAsync(ct)
            ?? program.ManualInitialTdeeKcal
            ?? throw new BadRequestException(ExceptionMessages.NoExpenditureEstimate);

        program.ExpenditureStartDate = today;

        var snapshot = await _uow.Repository<ExpenditureSnapshot>().FindTrackedAsync(s => s.UserId == userId && s.SnapshotDate == today, ct);
        if (snapshot is null)
        {
            snapshot = new ExpenditureSnapshot { Id = Guid.NewGuid(), UserId = userId, SnapshotDate = today };
            await _uow.Repository<ExpenditureSnapshot>().AddAsync(snapshot, ct);
        }
        snapshot.EstimatedTdeeKcal = latestTdee;
        snapshot.Status = ExpenditureStatus.Calibrating;
        snapshot.AvgCaloriesConsumed = 0;
        snapshot.WeightChangeRateKgPerWeek = null;
        snapshot.WindowDays = 0;

        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
