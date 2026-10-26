namespace Application;

public sealed class BulkMoveEntriesCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<BulkMoveEntriesCommand, int>
{
    public async Task<int> Handle(BulkMoveEntriesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var (selected, logs) = await BulkEntrySupport.LoadOwnedAsync(_uow, userId, request.Ids, ct);
        if (selected.Count == 0) return 0;

        var touched = logs.Where(l => l.Entries.Any(e => selected.Contains(e))).ToDictionary(l => l.LogDate);
        FoodLog? target = null;
        if (request.ToDate is { } toDate)
        {
            target = touched.TryGetValue(toDate, out var same) ? same : await FoodLogSupport.GetOrCreateAsync(_uow, userId, toDate, ct);
            touched[toDate] = target;
        }

        foreach (var e in selected)
        {
            if (request.ToTime is { } t)
            {
                e.EatenAt = t;
                e.MealType = request.ToMealType ?? MealTimes.ForTime(t);
            }
            else if (request.ToMealType is { } m)
            {
                e.MealType = m;
            }

            if (target is not null && e.FoodLogId != target.Id)
            {
                e.FoodLog.Entries.Remove(e);
                target.Entries.Add(e); // EF đổi FoodLogId theo navigation
                target.IsFasting = false;
            }
        }
        foreach (var l in touched.Values)
        {
            FoodLogSupport.RecalcTotals(l);
            NutritionDataSupport.ClearStaleIncomplete(l, NutritionDataSupport.Live(l));   // ngày nguồn vừa bị chuyển hết món ⇒ bỏ cờ chưa-đủ cũ (R2-4), cùng SaveChanges
        }

        var dates = touched.Keys.ToList();
        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, dates.Min(), _options.Value, _timeProvider, ct, dates);
        return selected.Count;
    }
}
