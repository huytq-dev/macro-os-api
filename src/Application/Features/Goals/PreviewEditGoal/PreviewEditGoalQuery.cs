namespace Application;

/// <summary>
/// Xem trước Edit Goal, không ghi DB: loại goal suy ra, ngân sách khởi điểm, ngày dự kiến, tốc độ tuần/tháng, dải Slow/Standard/Fast.
/// <paramref name="TargetWeightKg"/> null = không đặt đích (giữ cân); <paramref name="WeeklyRatePercent"/> là độ lớn % cân nặng/tuần, null = giữ tốc độ hiện tại hoặc mặc định.
/// </summary>
public sealed record PreviewEditGoalQuery(decimal? TargetWeightKg, decimal? WeeklyRatePercent) : IRequest<GoalEditPreviewResponse>;

public sealed class PreviewEditGoalQueryValidator : AbstractValidator<PreviewEditGoalQuery>
{
    public PreviewEditGoalQueryValidator()
    {
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.Range).When(x => x.TargetWeightKg.HasValue);
        RuleFor(x => x.WeeklyRatePercent).InclusiveBetween(0m, 1.5m).WithMessage(ValidationMessages.Range).When(x => x.WeeklyRatePercent.HasValue);
    }
}

public sealed class PreviewEditGoalQueryHandler(IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<PreviewEditGoalQuery, GoalEditPreviewResponse>
{
    public async Task<GoalEditPreviewResponse> Handle(PreviewEditGoalQuery request, CancellationToken ct)
    {
        var (state, _, _) = await GoalEditSupport.LoadAsync(_uow, _userContext.UserId, tracking: false, ct);
        return GoalEditCalculator.Compute(state, request.TargetWeightKg, request.WeeklyRatePercent, _options.Value, FoodLogSupport.Today(_timeProvider)).Preview;
    }
}
