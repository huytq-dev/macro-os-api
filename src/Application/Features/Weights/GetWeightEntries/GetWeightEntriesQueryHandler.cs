namespace Application;

public sealed class GetWeightEntriesQueryHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    TimeProvider _timeProvider
) : IRequestHandler<GetWeightEntriesQuery, WeightEntriesResponse>
{
    public async Task<WeightEntriesResponse> Handle(GetWeightEntriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var to = request.To ?? today;
        var from = request.From ?? to.AddDays(-90);

        var items = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate >= from && w.LogDate <= to)
            .OrderByDescending(w => w.LogDate)
            .ProjectToType<WeightEntryModel>()
            .ToListAsync(ct);

        var latest = items.FirstOrDefault();
        var weekAgo = latest is null ? null
            : items.FirstOrDefault(i => i.TrendWeightKg != null && i.LogDate <= latest.LogDate.AddDays(-7));

        var rate = latest?.TrendWeightKg is { } lt && weekAgo?.TrendWeightKg is { } wt
            ? WeightTrendCalculator.RatePerWeek(weekAgo.LogDate, wt, latest.LogDate, lt)
            : null;

        return new WeightEntriesResponse(items, latest?.WeightKg, latest?.TrendWeightKg, rate);
    }
}
