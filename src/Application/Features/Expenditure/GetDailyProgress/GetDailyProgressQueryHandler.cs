namespace Application;

public sealed class GetDailyProgressQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetDailyProgressQuery, IReadOnlyList<DailyProgressModel>>
{
    public async Task<IReadOnlyList<DailyProgressModel>> Handle(GetDailyProgressQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var to = request.To ?? FoodLogSupport.Today(_timeProvider);
        var from = request.From ?? to.AddDays(-30);
        var days = await _uow.Repository<DailyProgressSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .OrderBy(s => s.SnapshotDate)
            .ProjectToType<DailyProgressModel>()
            .ToListAsync(ct);

        // TDEE từng ngày (Energy Balance chế độ Expenditure) nằm ở ExpenditureSnapshot cùng ngày
        var tdee = await _uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= from && s.SnapshotDate <= to)
            .ToDictionaryAsync(s => s.SnapshotDate, s => (decimal?)s.EstimatedTdeeKcal, ct);

        return days.Select(d => d with { EstimatedTdeeKcal = tdee.GetValueOrDefault(d.SnapshotDate) }).ToList();
    }
}
