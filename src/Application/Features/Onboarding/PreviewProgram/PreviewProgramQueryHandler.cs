namespace Application;

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
