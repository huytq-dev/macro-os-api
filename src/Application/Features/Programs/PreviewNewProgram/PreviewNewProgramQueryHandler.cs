namespace Application;

public sealed class PreviewNewProgramQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<PreviewNewProgramQuery, ProgramDraftPreviewResponse>
{
    public async Task<ProgramDraftPreviewResponse> Handle(PreviewNewProgramQuery request, CancellationToken ct)
    {
        var draft = request.Draft;
        var today = (await UserLocale.GetAsync(_uow, _userContext.UserId, _timeProvider, ct)).Today;   // ngày local của user
        var (profile, weight) = await OnboardingData.LoadAsync(_uow, _userContext.UserId, ct);

        // TDEE cùng resolver với Setup và CreateProgram; TrainingType trong prefs chỉ ảnh hưởng mức protein (BuildProgram), không ảnh hưởng TDEE
        var resolved = await InitialTdeeResolver.ResolveAsync(_uow, _userContext.UserId, profile, weight, draft.ManualInitialTdeeKcal, today, _options.Value, ct);
        var tdee = resolved.TdeeKcal;
        var program = ProgramDraftCalculator.BuildProgram(draft, profile, weight, today);
        // Xem trước KHÔNG ném khi target không khả thi: trả cảnh báo có mã lý do + đề xuất trong Feasibility để user chỉnh trước khi Create (Create vẫn từ chối 400)
        var (days, feasibility) = ProgramDraftCalculator.TryDays(draft, program, weight, tdee, _options.Value);

        return ProgramDraftCalculator.Preview(days, tdee, weight, draft.TargetWeightKg, program.CalorieFloorKcal, _options.Value.KcalPerKg, today, resolved.Source, resolved.IsStale, feasibility);
    }
}
