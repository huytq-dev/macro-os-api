namespace Application;

public sealed class UpsertStepEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<UpsertStepEntryCommand, Guid>
{
    public async Task<Guid> Handle(UpsertStepEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.LogDate ?? FoodLogSupport.Today(_timeProvider);

        var entry = await _uow.Repository<StepEntry>().FindTrackedAsync(s => s.UserId == userId && s.LogDate == date, ct);
        if (entry is null)
        {
            entry = new StepEntry { Id = Guid.NewGuid(), UserId = userId, LogDate = date };
            await _uow.Repository<StepEntry>().AddAsync(entry, ct);
        }
        entry.Steps = request.Steps;
        entry.Source = StepSource.Manual;

        await _uow.SaveChangesAsync(ct);
        return entry.Id;
    }
}
