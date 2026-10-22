namespace Application;

public sealed class BulkCopyEntriesCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<BulkCopyEntriesCommand, int>
{
    public async Task<int> Handle(BulkCopyEntriesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var source = await _uow.Repository<FoodLogEntry>().Query()
            .Where(e => request.Ids.Contains(e.Id) && e.FoodLog.UserId == userId)
            .OrderBy(e => e.EatenAt).ThenBy(e => e.CreatedAt)
            .ToListAsync(ct);
        if (source.Count == 0) return 0;

        var dates = request.TargetDates();
        var targets = await FoodLogSupport.GetOrCreateManyAsync(_uow, userId, dates, ct);
        var copies = new List<(Guid Source, Guid Copy)>(source.Count * dates.Count); // entry nguồn → bản sao, để copy snapshot nutrient
        foreach (var date in dates)
        {
            var target = targets[date];
            foreach (var s in source)
            {
                var copy = new FoodLogEntry
                {
                    Id = Guid.NewGuid(),
                    FoodLogId = target.Id,
                    EatenAt = request.ToTime ?? s.EatenAt,
                    MealType = request.ToMealType ?? (request.ToTime is { } t ? MealTimes.ForTime(t) : s.MealType),
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
                copies.Add((s.Id, copy.Id));
            }
        }
        await NutrientSnapshot.CopyAsync(_uow, copies, ct);
        foreach (var target in targets.Values) FoodLogSupport.RecalcTotals(target);

        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, dates.Min(), _options.Value, _timeProvider, ct, dates);
        return copies.Count;
    }
}
