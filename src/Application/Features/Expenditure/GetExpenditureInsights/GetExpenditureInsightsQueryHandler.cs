namespace Application;

public sealed class GetExpenditureInsightsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetExpenditureInsightsQuery, ExpenditureInsightsResponse>
{
    public async Task<ExpenditureInsightsResponse> Handle(GetExpenditureInsightsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var opt = _options.Value;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var (raw, start) = await ExpenditureSeriesSupport.LoadAsync(_uow, userId, today, opt, ct);

        // 7 ngày gần nhất theo đúng quy tắc của ExpenditureRebuilder: log hợp lệ = có entry/nhịn ăn và không đánh dấu chưa đủ
        var d7 = today.AddDays(-6);
        var logs = await FoodLogSupport.LoadDaysAsync(_uow, userId, d7, today, ct, includeFasting: true);
        var weighed = await _uow.Repository<WeightEntry>().Query()
            .Where(x => x.UserId == userId && x.LogDate >= d7 && x.LogDate <= today)
            .Select(x => x.LogDate).ToListAsync(ct);
        var last7 = Enumerable.Range(0, 7).Select(i => d7.AddDays(i))
            .Select(d => new ExpenditureDay(d, logs.TryGetValue(d, out var l) && !l.IsIncomplete ? l.TotalCalories : null, weighed.Contains(d) ? 0 : null)).ToList();

        return ExpenditureSeries.Insights(raw, today, start, opt.CalibratingDays, last7);
    }
}
