namespace Application;

public sealed class AddWaterCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<AddWaterCommand, Guid>
{
    public async Task<Guid> Handle(AddWaterCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.LogDate ?? FoodLogSupport.Today(_timeProvider);
        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, date, ct);
        var (time, _) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, date, request.Time, null, _timeProvider, ct);

        var water = new WaterLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodLogId = log.Id,
            AmountMl = request.AmountMl,
            LoggedAt = new DateTimeOffset(date.ToDateTime(time), TimeSpan.Zero),
            Source = string.IsNullOrWhiteSpace(request.Source) ? null : request.Source.Trim(),
        };
        await _uow.Repository<WaterLog>().AddAsync(water, ct);
        log.TotalWater = await _uow.Repository<WaterLog>().Query().Where(w => w.FoodLogId == log.Id).SumAsync(w => w.AmountMl, ct) + request.AmountMl;

        await WaterSupport.SaveAsync(_uow, userId, date, _timeProvider, ct);
        return water.Id;
    }
}
