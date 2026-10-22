namespace Application;

public sealed class PreviewProgramQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<PreviewProgramQuery, ProgramPreviewResponse>
{
    public async Task<ProgramPreviewResponse> Handle(PreviewProgramQuery request, CancellationToken ct)
    {
        var (profile, weight) = await OnboardingData.LoadAsync(_uow, _userContext.UserId, ct);
        var today = (await UserLocale.GetAsync(_uow, _userContext.UserId, _timeProvider, ct)).Today;   // ngày local của user

        // Cùng resolver với Setup/Preview/Create: expenditure đã học thắng TDEE nhập tay, nhập tay thắng công thức
        var resolved = await InitialTdeeResolver.ResolveAsync(_uow, _userContext.UserId, profile, weight, request.ManualTdeeKcal, today, _options.Value, ct);
        var input = new ProgramPreviewInput(
            request.GoalType, request.TargetWeightKg, request.WeeklyRatePercent, request.DietStyle, request.ProteinLevel,
            request.CalorieFloor, request.TrainingType, request.ShiftMode, request.ShiftDays, request.ShiftPercent, resolved.TdeeKcal, request.ProgramType);
        return ProgramPreviewCalculator.Compute(profile, weight, input, _options.Value.KcalPerKg, today, await ExpenditureSettingsSupport.BmrSexAsync(_uow, _userContext.UserId, ct));
    }
}
