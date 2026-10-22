namespace Application;

public sealed class CopyFoodLogCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<CopyFoodLogCommand, int>
{
    public async Task<int> Handle(CopyFoodLogCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;

        var source = await _uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodLog.UserId == userId && e.FoodLog.LogDate == request.FromDate)
            .Where(e => request.MealType == null || e.MealType == request.MealType)
            .ToListAsync(ct);
        if (source.Count == 0) return 0;

        var target = await FoodLogSupport.GetOrCreateAsync(_uow, userId, request.ToDate, ct);
        var copied = new Dictionary<Guid, Guid>(); // entry nguồn → entry đích, để copy snapshot nutrient
        foreach (var s in source)
        {
            var copy = new FoodLogEntry
            {
                Id = Guid.NewGuid(),
                FoodLogId = target.Id,
                MealType = request.ToMealType ?? s.MealType,
                EatenAt = s.EatenAt,
                FoodItemId = s.FoodItemId,
                RecipeId = s.RecipeId,
                RecipeVersionId = s.RecipeVersionId,
                Source = s.Source,
                FoodServingId = s.FoodServingId,
                Quantity = s.Quantity,
                Calories = s.Calories, Protein = s.Protein, Carbs = s.Carbs, Fat = s.Fat,
                Fiber = s.Fiber, Sodium = s.Sodium,
                ServingSnapshotJson = s.ServingSnapshotJson,
            };
            await FoodLogSupport.AddEntryAsync(_uow, target, copy, ct);
            copied[s.Id] = copy.Id;
        }
        await NutrientSnapshot.CopyAsync(_uow, copied, ct);
        FoodLogSupport.RecalcTotals(target);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, request.ToDate, _options.Value, _timeProvider, ct);
        return source.Count;
    }
}
