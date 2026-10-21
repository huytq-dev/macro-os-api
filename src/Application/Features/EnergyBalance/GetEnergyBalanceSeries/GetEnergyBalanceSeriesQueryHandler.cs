namespace Application;

public sealed class GetEnergyBalanceSeriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetEnergyBalanceSeriesQuery, EnergyBalanceSeriesResponse>
{
    public async Task<EnergyBalanceSeriesResponse> Handle(GetEnergyBalanceSeriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To is { } t && t < locale.Today ? t : locale.Today;
        DateOnly? from = request.From ?? (request.All == true ? null : to.AddDays(-90));

        var (first, days) = await EnergyBalanceSupport.LoadAsync(_uow, userId, from, to, _options.Value, ct);
        var summary = EnergyBalanceCalculator.Summary(days, first, to);
        var buckets = request.Granularity is { } g ? EnergyBalanceCalculator.Buckets(days, g, locale.WeekStart) : [];
        return new EnergyBalanceSeriesResponse(first, to, days, summary, buckets);
    }
}
