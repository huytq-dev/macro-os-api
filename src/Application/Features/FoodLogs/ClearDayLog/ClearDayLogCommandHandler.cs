namespace Application;

public sealed class ClearDayLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<ClearDayLogCommand, Unit>
{
    public async Task<Unit> Handle(ClearDayLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var log = await _uow.Repository<FoodLog>().FindTrackedAsync(l => l.UserId == userId && l.LogDate == request.Date, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLog", request.Date));
        await _uow.Repository<FoodLog>().RemoveAsync(log, ct);
        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.Date, _options.Value, _timeProvider, ct);
        return Unit.Value;
    }
}
