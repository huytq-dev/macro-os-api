namespace Application;

public static class ExpenditureSeriesSupport
{
    /// <summary>Snapshot đã lưu + ExpenditureStartDate của program đang chạy; chuỗi liên tục đến <paramref name="today"/>.</summary>
    public static async Task<(List<ExpenditureSeriesPoint> Raw, DateOnly? Start)> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly today, AlgorithmOptions opt, CancellationToken ct)
    {
        var rows = (await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate <= today)
            .Select(s => new { s.SnapshotDate, s.EstimatedTdeeKcal, s.Status, s.WindowDays })
            .ToListAsync(ct)).Select(s => new ExpenditureSnapshotRow(s.SnapshotDate, s.EstimatedTdeeKcal, s.Status.ToString(), s.WindowDays));

        var start = await uow.Repository<MacroProgram>().Query()
            .Where(p => p.UserId == userId && p.IsActive)
            .Select(p => (DateOnly?)p.ExpenditureStartDate)
            .FirstOrDefaultAsync(ct);
        return (ExpenditureSeries.Daily(rows, today, start, opt.CalibratingDays), start);
    }
}

/// <summary>
/// Trang Expenditure: TDEE MỖI NGÀY từ snapshot đầu tiên đến To (mặc định và tối đa hôm nay theo múi giờ user; ngày chưa có snapshot giữ TDEE gần nhất), kèm Flux Range,
/// thay đổi so với hôm trước, thống kê (Average, Difference) và bucket Day/Week/Month khi có Granularity. From mặc định 90 ngày trước To; All = true và không From: từ ngày đầu tiên của chuỗi.
/// </summary>
public sealed record GetExpenditureSeriesQuery(DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<ExpenditureSeriesResponse>;

public sealed class GetExpenditureSeriesQueryValidator : AbstractValidator<GetExpenditureSeriesQuery>
{
    public GetExpenditureSeriesQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage("From must not be after To.");
}

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
        var days = ExpenditureSeries.ToDays(raw, opt.KcalPerKg, opt.FluxTrendErrorKg).Where(d => d.Date >= from).ToList();

        var (summary, buckets) = MetricSeries.Build(days.Select(d => (d.Date, d.ExpenditureKcal)), from, to, request.Granularity, locale.WeekStart);
        return new ExpenditureSeriesResponse(from, to, days, summary, buckets);
    }
}

/// <summary>
/// Insights &amp; Data của trang Expenditure: TDEE hôm nay, Strategy (Calibrating | Updating | Holding) và lý do Holding (thiếu log / không cân trong 7 ngày),
/// thay đổi so với 3/7/14/30/90 ngày trước (Waiting khi mốc nằm trước ngày đầu của chuỗi). Tính đến hôm nay theo múi giờ user.
/// </summary>
public sealed record GetExpenditureInsightsQuery : IRequest<ExpenditureInsightsResponse>;

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
