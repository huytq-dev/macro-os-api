namespace Application;

public sealed class GetMeasurementOverviewQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetMeasurementOverviewQuery, MeasurementOverviewResponse>
{
    public async Task<MeasurementOverviewResponse> Handle(GetMeasurementOverviewQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var count = Math.Clamp(request.Entries ?? 7, 1, 30);
        var rows = await _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && b.Method == MeasurementMethod.Tape)
            .OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        var metrics = new List<MeasurementOverviewItem>();
        foreach (var m in BodyMeasurements.All)
        {
            var entries = rows.Where(r => m.Get(r) is not null).GroupBy(r => r.MeasuredDate)
                .Select(g => new MeasurementEntryModel(g.Key, m.Get(g.First())!.Value)).Take(count).ToList();
            if (entries.Count > 0) metrics.Add(new MeasurementOverviewItem(m.Key, m.Group, entries[0].Date, entries[0].ValueCm, entries));
        }
        return new MeasurementOverviewResponse(metrics);
    }
}
