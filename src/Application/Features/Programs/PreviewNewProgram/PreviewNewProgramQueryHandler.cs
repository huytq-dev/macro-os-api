namespace Application;

public sealed class PreviewNewProgramQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<PreviewNewProgramQuery, ProgramDraftPreviewResponse>
{
    public async Task<ProgramDraftPreviewResponse> Handle(PreviewNewProgramQuery request, CancellationToken ct)
    {
        var draft = request.Draft;
        var today = FoodLogSupport.Today(_timeProvider);
        var (profile, weight) = await OnboardingData.LoadAsync(_uow, _userContext.UserId, ct);

        // TDEE cùng công thức với CreateProgram; TrainingType trong prefs chỉ ảnh hưởng mức protein (BuildProgram), không ảnh hưởng TDEE
        var tdee = draft.ManualInitialTdeeKcal ?? OnboardingCalculator.EstimateTdee(profile, weight, today).TdeeKcal;
        var program = ProgramDraftCalculator.BuildProgram(draft, profile, weight, today);
        var days = ProgramDraftCalculator.Days(draft, program, weight, tdee, _options.Value);

        return ProgramDraftCalculator.Preview(days, tdee, weight, draft.TargetWeightKg, program.CalorieFloorKcal, _options.Value.KcalPerKg, today);
    }
}
