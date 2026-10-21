namespace Application;

public sealed class GetWeightTrendQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetWeightTrendQuery, WeightTrendResponse>
{
    public async Task<WeightTrendResponse> Handle(GetWeightTrendQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To is { } t && t < locale.Today ? t : locale.Today;
        var trendDays = _options.Value.TrendDays;

        // Nguồn duy nhất: các lần cân chưa xoá (Scale Weight); sửa/xoá cân thì lần gọi sau tự tính lại
        var entries = (await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate <= to)
            .Select(w => new { w.LogDate, w.WeightKg })
            .ToListAsync(ct)).Select(w => (w.LogDate, w.WeightKg)).ToList();

        var raw = WeightTrendSeries.Daily(entries, trendDays, to);
        var from = request.From ?? (request.All == true ? (raw.Count > 0 ? raw[0].Date : to) : to.AddDays(-90));
        var days = WeightTrendSeries.ToDays(raw).Where(d => d.Date >= from).ToList();

        var (summary, _) = MetricSeries.Build(days.Select(d => (d.Date, d.TrendKg)), from, to, null, locale.WeekStart);
        var (scaleSummary, _) = MetricSeries.Build(entries, from, to, null, locale.WeekStart);

        var buckets = new List<WeightTrendBucket>();
        if (request.Granularity is { } g)
        {
            var scales = entries.Where(e => e.LogDate >= from && e.LogDate <= to).ToLookup(e => MetricSeries.BucketStart(e.LogDate, g, locale.WeekStart), e => e.WeightKg);
            buckets = days.GroupBy(d => MetricSeries.BucketStart(d.Date, g, locale.WeekStart)).OrderBy(b => b.Key)
                .Select(b => new WeightTrendBucket(b.Key, MetricSeries.BucketEnd(b.Key, g), Math.Round(b.Average(d => d.TrendKg), 2, MidpointRounding.AwayFromZero),
                    scales[b.Key].Any() ? Math.Round(scales[b.Key].Average(), 2, MidpointRounding.AwayFromZero) : null, b.Count(), scales[b.Key].Count()))
                .ToList();
        }

        return new WeightTrendResponse(trendDays, Math.Round(2m / (trendDays + 1), 4), from, to, days, summary, scaleSummary, buckets);
    }
}
