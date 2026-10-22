namespace Application;

public sealed class EditGoalCommandHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<EditGoalCommand, GoalEditPreviewResponse>
{
    public async Task<GoalEditPreviewResponse> Handle(EditGoalCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await _clock.GetAsync(ct)).Today;
        var (state, goal, program) = await GoalEditSupport.LoadAsync(_uow, userId, tracking: true, ct);
        var result = GoalEditCalculator.Compute(state, request.TargetWeightKg, request.WeeklyRatePercent, _options.Value, today);
        if (result.Preview.RequiresNewProgram) return result.Preview; // không đổi gì

        goal ??= await GoalSupport.EnsureActiveGoalAsync(_uow, userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveGoal);
        goal.TargetWeightKg = request.TargetWeightKg;
        goal.RatePerWeekKg = result.SignedWeeklyKg;
        goal.Type = result.NewGoalType;

        program.GoalType = result.NewGoalType;
        program.WeeklyWeightChangeTargetPercent = result.ProgramRatePercent;
        program.WeeklyWeightChangeTargetKg = result.SignedWeeklyKg;

        // Manual không có thuật toán tính target: chỉ đổi goal. Còn lại tính lại tuần hiệu lực từ hôm nay (sàn kcal, chia lại 7 ngày nếu có DayTarget)
        if (program.ProgramType != ProgramType.Manual)
        {
            var tdee = state.TdeeKcal ?? throw new BadRequestException(ExceptionMessages.NoExpenditureEstimate);
            var targets = ProgramWeekBuilder.Compute(program, state.CurrentWeightKg, tdee, _options.Value);
            ProgramWeekBuilder.UpsertWeek(program, today, targets, AdjustmentReason.Manual);
        }

        await _uow.EnsureSaveAsync(ct);
        return result.Preview;
    }
}
