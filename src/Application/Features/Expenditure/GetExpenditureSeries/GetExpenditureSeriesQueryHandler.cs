namespace Application;

public sealed class GetExpenditureSeriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetExpenditureSeriesQuery, ExpenditureSeriesResponse>
{
    public async Task<ExpenditureSeriesResponse> Handle(GetExpenditureSeriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var opt = _options.Value;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To is { } t && t < locale.Today ? t : locale.Today;

        var (raw, _) = await ExpenditureSeriesSupport.LoadAsync(_uow, userId, to, opt, ct);
        var from = request.From ?? (request.All == true ? (raw.Count > 0 ? raw[0].Date : to) : to.AddDays(-90));
        var days = ExpenditureSeries.ToDays(raw).Where(d => d.Date >= from).ToList();

        var (summary, buckets) = MetricSeries.Build(days.Select(d => (d.Date, d.ExpenditureKcal)), from, to, request.Granularity, locale.WeekStart);
        return new ExpenditureSeriesResponse(from, to, days, summary, buckets);
    }
}
