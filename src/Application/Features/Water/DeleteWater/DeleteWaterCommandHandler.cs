namespace Application;

public sealed class DeleteWaterCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<DeleteWaterCommand, Unit>
{
    public async Task<Unit> Handle(DeleteWaterCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var water = await _uow.Repository<WaterLog>().Query(tracking: true)
            .Include(w => w.FoodLog)
            .FirstOrDefaultAsync(w => w.Id == request.Id && w.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("WaterLog", request.Id));

        var log = water.FoodLog;
        log.TotalWater = await _uow.Repository<WaterLog>().Query().Where(w => w.FoodLogId == log.Id && w.Id != water.Id).SumAsync(w => w.AmountMl, ct);
        await _uow.Repository<WaterLog>().RemoveAsync(water, ct);

        await WaterSupport.SaveAsync(_uow, userId, log.LogDate, _timeProvider, ct);
        return Unit.Value;
    }
}
