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

        // 7 ngày gần nhất theo đúng quy tắc của ExpenditureRebuilder: log hợp lệ = nhịn ăn, hoặc có entry với tổng > 0 kcal, và không đánh dấu chưa đủ
        var d7 = today.AddDays(-7);   // 8 ngày: đủ 7 ngày log kết thúc hôm qua khi hôm nay chưa xong
        var logs = await FoodLogSupport.LoadDaysAsync(_uow, userId, d7, today, ct, includeFasting: true);
        var weighed = await _uow.Repository<WeightEntry>().Query()
            .Where(x => x.UserId == userId && x.LogDate >= d7 && x.LogDate <= today)
            .Select(x => x.LogDate).ToListAsync(ct);
        var last7 = Enumerable.Range(0, 8).Select(i => d7.AddDays(i))
            .Select(d => new ExpenditureDay(d, FoodLogSupport.AlgorithmKcalOn(logs, d), weighed.Contains(d) ? 0 : null)).ToList();

        return ExpenditureSeries.Insights(raw, today, start, opt.CalibratingDays, last7, opt.FreezeTdeeDuringCalibration);
    }
}
