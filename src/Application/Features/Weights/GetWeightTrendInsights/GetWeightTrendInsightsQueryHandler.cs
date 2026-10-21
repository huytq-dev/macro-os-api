namespace Application;

public sealed class GetWeightTrendInsightsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetWeightTrendInsightsQuery, WeightTrendInsightsResponse>
{
    public async Task<WeightTrendInsightsResponse> Handle(GetWeightTrendInsightsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var entries = (await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId && w.LogDate <= today)
            .Select(w => new { w.LogDate, w.WeightKg })
            .ToListAsync(ct)).Select(w => (w.LogDate, w.WeightKg)).ToList();

        var raw = WeightTrendSeries.Daily(entries, _options.Value.TrendDays, today);
        return WeightTrendSeries.Insights(raw, today, _options.Value.TrendDays, _options.Value.KcalPerKg);
    }
}
