namespace Application;

public sealed class PreviewEditGoalQueryHandler(IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<PreviewEditGoalQuery, GoalEditPreviewResponse>
{
    public async Task<GoalEditPreviewResponse> Handle(PreviewEditGoalQuery request, CancellationToken ct)
    {
        var (state, _, _) = await GoalEditSupport.LoadAsync(_uow, _userContext.UserId, tracking: false, ct);
        return GoalEditCalculator.Compute(state, request.TargetWeightKg, request.WeeklyRatePercent, _options.Value, FoodLogSupport.Today(_timeProvider)).Preview;
    }
}
