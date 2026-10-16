namespace Application;

public sealed class DeleteWeightEntryCommandHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider
) : IRequestHandler<DeleteWeightEntryCommand, Unit>
{
    public async Task<Unit> Handle(DeleteWeightEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var entry = await _uow.Repository<WeightEntry>().FindTrackedAsync(w => w.UserId == userId && w.LogDate == request.LogDate, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFoundField("WeightEntry", "LogDate", request.LogDate));

        await _uow.Repository<WeightEntry>().RemoveAsync(entry, ct); // interceptor chuyển thành soft delete
        await _uow.SaveChangesAsync(ct);
        await WeightTrendRefresher.RefreshAsync(_uow, userId, _options.Value.TrendDays, ct);
        await _uow.SaveChangesAsync(ct);
        await ExpenditureRebuilder.RebuildAsync(_uow, userId, request.LogDate, FoodLogSupport.Today(_timeProvider), _options.Value, ct);
        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
