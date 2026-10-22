namespace Application;

public sealed class QuickAddCommandHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<QuickAddCommand, Guid>
{
    public async Task<Guid> Handle(QuickAddCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;

        var food = new FoodItem
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(request.Name) ? "Quick add" : request.Name.Trim(),
            Category = FoodCategory.Other,
            Source = FoodSource.QuickAdd,
            CreatedByUserId = userId,
        };
        FoodNutritionApplier.Apply(food, new FoodNutrition(request.Calories, request.Protein, request.Carbs, request.Fat));
        await _uow.Repository<FoodItem>().AddAsync(food, ct);

        var log = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.LogDate ?? (await _clock.GetAsync(ct)).Today, ct);
        var (eatenAt, meal) = await FoodLogSupport.ResolveTimeAsync(_uow, userId, log.LogDate, request.EatenAt, request.MealType, _timeProvider, ct);
        var entry = new FoodLogEntry
        {
            Id = Guid.NewGuid(),
            FoodLogId = log.Id,
            MealType = meal,
            EatenAt = eatenAt,
            FoodItemId = food.Id,
            Quantity = 100, // gram; /100 g × 100 g = đúng giá trị nhập
        };
        FoodLogSupport.Snapshot(entry, food, 100);
        EntryServingSnapshot.Stamp(entry, null, null, null, 100);
        await FoodLogSupport.AddEntryAsync(_uow, log, entry, ct);
        FoodLogSupport.RecalcTotals(log);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, log.LogDate, _options.Value, _timeProvider, ct);
        return entry.Id;
    }
}
