namespace Application;

/// <summary>
/// Áp dụng Edit Goal trong MỘT lần lưu: Goal (đích, tốc độ) + MacroProgram (rate) + target tuần hiện tại (Coached/Collaborative tính lại từ hôm nay; Manual không tính).
/// Đổi hướng goal (Loss ⇄ Gain ⇄ Maintenance) → <c>RequiresNewProgram = true</c> và KHÔNG đổi gì: FE chuyển sang wizard New Program.
/// Thay cặp UpdateGoalTarget + UpdateProgramGoal mà FE gọi rời (hai cái cũ vẫn giữ cho client cũ). Trả bản xem trước (changes = từ → tới) của lần sửa này.
/// </summary>
public sealed record EditGoalCommand(decimal? TargetWeightKg, decimal? WeeklyRatePercent) : IRequest<GoalEditPreviewResponse>;

public sealed class EditGoalCommandValidator : AbstractValidator<EditGoalCommand>
{
    public EditGoalCommandValidator()
    {
        RuleFor(x => x.TargetWeightKg).InclusiveBetween(20m, 400m).WithMessage(ValidationMessages.Range).When(x => x.TargetWeightKg.HasValue);
        RuleFor(x => x.WeeklyRatePercent).InclusiveBetween(0m, 1.5m).WithMessage(ValidationMessages.Range).When(x => x.WeeklyRatePercent.HasValue);
    }
}

public sealed class EditGoalCommandHandler(IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<EditGoalCommand, GoalEditPreviewResponse>
{
    public async Task<GoalEditPreviewResponse> Handle(EditGoalCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = FoodLogSupport.Today(_timeProvider);
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
