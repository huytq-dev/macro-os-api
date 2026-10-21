namespace Application;

public sealed class GetVisualBodyFatQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetVisualBodyFatQuery, VisualBodyFatResponse>
{
    public async Task<VisualBodyFatResponse> Handle(GetVisualBodyFatQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To ?? locale.Today;
        var from = request.From ?? (request.All == true ? DateOnly.MinValue : to.AddDays(-90));

        var rows = await _uow.Repository<BodyMetric>().Query()
            .Where(b => b.UserId == userId && b.Method == MeasurementMethod.Visual && b.BodyFatPercent != null && b.MeasuredDate >= from && b.MeasuredDate <= to)
            .OrderByDescending(b => b.MeasuredDate).ThenByDescending(b => b.CreatedAt)
            .Select(b => new { b.Id, b.MeasuredDate, Percent = b.BodyFatPercent!.Value })
            .ToListAsync(ct);

        // Mỗi ngày một dòng (dữ liệu cũ có thể trùng ngày: lấy lần ghi mới nhất)
        var items = rows.GroupBy(r => r.MeasuredDate).Select(g => g.First())
            .Select(r => new VisualBodyFatEntryModel(r.Id, r.MeasuredDate, r.Percent)).ToList();

        if (request.From is null && request.All == true) from = items.Count == 0 ? to : items[^1].Date;
        var (summary, buckets) = MetricSeries.Build(items.Select(i => (i.Date, i.Percent)), from, to, request.Granularity, locale.WeekStart);
        var shown = request.Limit is { } limit ? items.Take(Math.Clamp(limit, 1, 1000)).ToList() : items;
        return new VisualBodyFatResponse(shown, summary, buckets, items.FirstOrDefault()?.Percent);
    }
}
