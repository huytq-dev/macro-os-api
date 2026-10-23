namespace Application;

public sealed class GetEnergyBalanceInsightsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetEnergyBalanceInsightsQuery, EnergyBalanceInsightsResponse>
{
    public async Task<EnergyBalanceInsightsResponse> Handle(GetEnergyBalanceInsightsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var (_, days) = await EnergyBalanceSupport.LoadAsync(_uow, userId, today.AddDays(-(EnergyBalanceCalculator.Windows.Max() - 1)), today, _options.Value, ct, today);
        return new EnergyBalanceInsightsResponse(today, EnergyBalanceCalculator.Relative(days, today, false), EnergyBalanceCalculator.Relative(days, today, true));
    }
}
