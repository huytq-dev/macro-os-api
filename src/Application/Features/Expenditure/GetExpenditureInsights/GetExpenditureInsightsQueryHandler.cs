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

        // 8 ngày trạng thái + cửa sổ tính, đúng dữ liệu và quy tắc của ExpenditureRebuilder (log hợp lệ = nhịn ăn, hoặc kcal > 0 và không đánh dấu chưa đủ; lần cân = có trend, từ ngày bắt đầu)
        var (statusDays, window) = await ExpenditureDiagnostics.LoadAsync(_uow, userId, today, start, opt, ct);

        // Flux hôm nay từ CÙNG điểm tính với series/GET /expenditure
        var bands = await ExpenditureFluxService.LoadBandsAsync(_uow, userId, raw, start, opt, ct);
        var flux = raw.Count > 0 && bands.TryGetValue(raw[^1].Date, out var b) ? b : null;
        return ExpenditureSeries.Insights(raw, today, start, opt.CalibratingDays, statusDays, opt.FreezeTdeeDuringCalibration, window, opt.ExpenditureWindowDays, flux);
    }
}
