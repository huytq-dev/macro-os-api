namespace Application;

/// <summary>
/// Xem trước goal/program ngay trong wizard (ngân sách kcal, ngày dự kiến đạt mục tiêu, macro 7 ngày, sàn kcal).
/// Dùng đúng công thức của CreateProgram nhưng không ghi gì.
/// </summary>
public sealed record PreviewProgramQuery(
    GoalType GoalType,
    decimal? TargetWeightKg,
    decimal WeeklyRatePercent,
    DietStyle DietStyle,
    ProteinLevel ProteinLevel,
    CalorieFloorChoice CalorieFloor,
    TrainingType TrainingType,
    CalorieShiftMode ShiftMode,
    List<DayOfWeek>? ShiftDays,
    decimal? ShiftPercent,
    int? ManualTdeeKcal
) : IRequest<ProgramPreviewResponse>;

public sealed class PreviewProgramQueryValidator : AbstractValidator<PreviewProgramQuery>
{
    public PreviewProgramQueryValidator()
    {
        RuleFor(x => x.GoalType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.DietStyle).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.ProteinLevel).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.CalorieFloor).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.TrainingType).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.ShiftMode).IsInEnum().WithMessage(ValidationMessages.InvalidEnumValue);
        RuleFor(x => x.WeeklyRatePercent).InclusiveBetween(0m, 1.5m).WithMessage(ValidationMessages.Range);
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.Range).When(x => x.TargetWeightKg.HasValue);
        RuleFor(x => x.ShiftPercent).InclusiveBetween(5m, 30m).WithMessage(ValidationMessages.Range).When(x => x.ShiftPercent.HasValue);
        RuleFor(x => x.ManualTdeeKcal).InclusiveBetween(1000, 6000).WithMessage(ValidationMessages.Range).When(x => x.ManualTdeeKcal.HasValue);
    }
}

public sealed class PreviewProgramQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<PreviewProgramQuery, ProgramPreviewResponse>
{
    public async Task<ProgramPreviewResponse> Handle(PreviewProgramQuery request, CancellationToken ct)
    {
        var (profile, weight) = await OnboardingData.LoadAsync(_uow, _userContext.UserId, ct);
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        var input = new ProgramPreviewInput(
            request.GoalType, request.TargetWeightKg, request.WeeklyRatePercent, request.DietStyle, request.ProteinLevel,
            request.CalorieFloor, request.TrainingType, request.ShiftMode, request.ShiftDays, request.ShiftPercent, request.ManualTdeeKcal);
        return ProgramPreviewCalculator.Compute(profile, weight, input, _options.Value.KcalPerKg, today);
    }
}
