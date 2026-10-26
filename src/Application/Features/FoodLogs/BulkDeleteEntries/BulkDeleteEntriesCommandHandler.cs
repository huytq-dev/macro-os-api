namespace Application;

public sealed class BulkDeleteEntriesCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<BulkDeleteEntriesCommand, int>
{
    public async Task<int> Handle(BulkDeleteEntriesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var (selected, logs) = await BulkEntrySupport.LoadOwnedAsync(_uow, userId, request.Ids, ct);
        if (selected.Count == 0) return 0;

        foreach (var e in selected)
        {
            await _uow.Repository<FoodLogEntry>().RemoveAsync(e, ct); // soft delete qua interceptor
            e.IsDeleted = true; // để RecalcTotals bỏ qua ngay trong cùng unit of work
        }
        var touched = logs.Where(l => l.Entries.Any(e => selected.Contains(e))).ToList();
        foreach (var l in touched)
        {
            FoodLogSupport.RecalcTotals(l);
            NutritionDataSupport.ClearStaleIncomplete(l, NutritionDataSupport.Live(l));   // ngày thành trống thật ⇒ bỏ cờ chưa-đủ cũ (R2-4), cùng SaveChanges
        }

        var dates = touched.Select(l => l.LogDate).Distinct().ToList();
        await FoodLogSupport.SaveAndRebuildAsync(_uow, userId, dates.Min(), _options.Value, _timeProvider, ct, dates);
        return selected.Count;
    }
}
