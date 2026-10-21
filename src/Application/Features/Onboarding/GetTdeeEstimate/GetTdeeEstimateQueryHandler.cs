namespace Application;

public sealed class GetTdeeEstimateQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetTdeeEstimateQuery, TdeeEstimateResponse>
{
    public async Task<TdeeEstimateResponse> Handle(GetTdeeEstimateQuery request, CancellationToken ct)
    {
        var (profile, weight) = await OnboardingData.LoadAsync(_uow, _userContext.UserId, ct);
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var estimate = OnboardingCalculator.EstimateTdee(profile, weight, today);
        return new TdeeEstimateResponse(estimate.TdeeKcal, estimate.BmrKcal, estimate.Method, weight);
    }
}
