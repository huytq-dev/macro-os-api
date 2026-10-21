namespace Application;

public sealed class RebuildNutritionAggregatesCommandHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<RebuildNutritionAggregatesCommand, int>
{
    public async Task<int> Handle(RebuildNutritionAggregatesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var to = request.To ?? FoodLogSupport.Today(_timeProvider);
        var from = request.From ?? to.AddDays(-364);

        var logged = await FoodLogSupport.LoadDaysAsync(_uow, userId, from, to, ct);
        // Ngày không còn entry nhưng còn aggregate cũ cũng phải dọn: thêm vào danh sách
        var stale = await _uow.Repository<NutritionDayAggregate>().Query()
            .Where(a => a.UserId == userId && a.LocalDate >= from && a.LocalDate <= to)
            .Select(a => a.LocalDate).ToListAsync(ct);
        var dates = logged.Keys.Union(stale).ToList();

        await NutritionAggregates.RefreshAsync(_uow, userId, dates, _timeProvider, ct);
        await _uow.SaveChangesAsync(ct);
        return logged.Count;
    }
}
