namespace Application;

public sealed class GetMeasurementSeriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetMeasurementSeriesQuery, MeasurementSeriesResponse>
{
    public async Task<MeasurementSeriesResponse> Handle(GetMeasurementSeriesQuery request, CancellationToken ct)
    {
        var m = BodyMeasurements.Find(request.Key) ?? throw new NotFoundException(ExceptionMessages.NotFound("Measurement", request.Key));
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To ?? locale.Today;
        var from = request.From ?? (request.All == true ? DateOnly.MinValue : to.AddDays(-90));

        var rows = await _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && b.Method == MeasurementMethod.Tape && b.MeasuredDate >= from && b.MeasuredDate <= to)
            .OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt)
            .ToListAsync(ct);

        // Mỗi ngày một giá trị (dữ liệu cũ có thể trùng ngày: lấy hàng mới nhất có giá trị)
        var all = rows.Where(r => m.Get(r) is not null).GroupBy(r => r.MeasuredDate)
            .Select(g => new MeasurementEntryModel(g.Key, m.Get(g.First())!.Value)).ToList();

        if (request.From is null && request.All == true) from = all.Count == 0 ? to : all[^1].Date;
        var (summary, buckets) = MetricSeries.Build(all.Select(i => (i.Date, i.ValueCm)), from, to, request.Granularity, locale.WeekStart);
        var items = request.Limit is { } limit ? all.Take(Math.Clamp(limit, 1, 1000)).ToList() : all;
        return new MeasurementSeriesResponse(m.Key, m.Group, items, summary, buckets, all.FirstOrDefault()?.ValueCm);
    }
}
