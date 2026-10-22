namespace Application;

public sealed class GetTdeeEstimateQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetTdeeEstimateQuery, TdeeEstimateResponse>
{
    public async Task<TdeeEstimateResponse> Handle(GetTdeeEstimateQuery request, CancellationToken ct)
    {
        var (profile, weight) = await OnboardingData.LoadAsync(_uow, _userContext.UserId, ct);
        var today = (await UserLocale.GetAsync(_uow, _userContext.UserId, _timeProvider, ct)).Today;   // ngày local của user (tính tuổi)
        var estimate = OnboardingCalculator.EstimateTdee(profile, weight, today, await ExpenditureSettingsSupport.BmrSexAsync(_uow, _userContext.UserId, ct));
        return new TdeeEstimateResponse(estimate.TdeeKcal, estimate.BmrKcal, estimate.Method, weight, estimate.BodyFatPercent);
    }
}
