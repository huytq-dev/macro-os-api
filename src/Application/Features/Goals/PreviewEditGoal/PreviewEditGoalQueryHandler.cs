namespace Application;

public sealed class PreviewEditGoalQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<PreviewEditGoalQuery, GoalEditPreviewResponse>
{
    public async Task<GoalEditPreviewResponse> Handle(PreviewEditGoalQuery request, CancellationToken ct)
    {
        var (state, _, program) = await GoalEditSupport.LoadAsync(_uow, _userContext.UserId, tracking: false, ct);
        var result = GoalEditCalculator.Compute(state, request.TargetWeightKg, request.WeeklyRatePercent, _options.Value, (await _clock.GetAsync(ct)).Today);
        var preview = result.Preview;
        if (!preview.RequiresNewProgram && program.ProgramType != ProgramType.Manual && state.TdeeKcal is { } tdee)
        {
            // Cùng kiểm khả thi với EditGoal (áp dụng): cảnh báo + mã lý do + đề xuất trước khi user bấm lưu. program không được track nên gán tạm chỉ phục vụ tính, không bao giờ lưu
            program.WeeklyWeightChangeTargetPercent = result.ProgramRatePercent;
            preview = preview with { Feasibility = ProgramWeekBuilder.Assess(program, ProgramWeekBuilder.Compute(program, state.CurrentWeightKg, tdee, _options.Value)) };
        }
        return preview;
    }
}
