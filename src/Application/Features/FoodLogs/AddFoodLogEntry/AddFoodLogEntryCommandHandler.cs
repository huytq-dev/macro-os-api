namespace Application;

public sealed class AddFoodLogEntryCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<AddFoodLogEntryCommand, Guid>
{
    public async Task<Guid> Handle(AddFoodLogEntryCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var line = new EntryLine(request.FoodItemId, request.RecipeId, request.FoodServingId, request.Quantity);
        var lookup = await FoodLogEntryFactory.LoadAsync(_uow, userId, [line], ct);

        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.LogDate ?? FoodLogSupport.Today(_timeProvider), ct);
        var (eatenAt, meal) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, log.LogDate, request.EatenAt, request.MealType, _timeProvider, ct);

        var entry = await FoodLogEntryFactory.AddAsync(_uow, lookup, line, log, eatenAt, meal, batchId: null, ct);
        FoodLogSupport.RecalcTotals(log);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);
        return entry.Id;
    }
}
