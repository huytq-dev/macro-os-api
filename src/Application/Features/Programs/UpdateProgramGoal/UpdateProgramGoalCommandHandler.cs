namespace Application;

public sealed class UpdateProgramGoalCommandHandler(
    IUnitOfWork _uow,
    IUserContext _userContext,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider
) : IRequestHandler<UpdateProgramGoalCommand, Unit>
{
    public async Task<Unit> Handle(UpdateProgramGoalCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;   // ngày local của user

        var program = await _uow.Repository<MacroProgram>().Query(tracking: true)
            .Include(p => p.Weeks)
            .Include(p => p.DayTargets)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct)
            ?? throw new NotFoundException(ExceptionMessages.NoActiveProgram);

        var profile = await _uow.Repository<UserProfile>().FindAsync(p => p.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("UserProfile", userId));

        if (request.GoalType.HasValue) program.GoalType = request.GoalType.Value;
        if (request.DietStyle.HasValue) program.DietStyle = request.DietStyle.Value;
        if (request.CheckInWeekday.HasValue) program.CheckInWeekday = request.CheckInWeekday.Value;
        if (request.CalorieFloorKcal.HasValue) program.CalorieFloorKcal = request.CalorieFloorKcal;
        if (request.FixedProteinGrams.HasValue) program.FixedProteinGrams = request.FixedProteinGrams;
        if (request.ProteinLevel.HasValue)
        {
            program.ProteinLevel = request.ProteinLevel.Value;
            program.ProteinGPerKgBodyweight = MacroTargetCalculator.ProteinGPerKg(program.ProteinLevel, profile.TrainingType);
        }

        // Xác nhận shift của user (đề xuất từ Feasibility: % nhỏ hơn hoặc chia đều): chỉ đổi khi user gửi; kiểm khả thi bên dưới (UpsertWeek / Assess) trước khi lưu
        if (request.CalorieShiftMode is { } shiftMode)
        {
            var shifted = shiftMode != CalorieShiftMode.Even;
            program.CalorieShiftMode = shiftMode;
            program.CalorieShiftPercent = shifted ? request.CalorieShiftPercent : null;
            program.CalorieShiftDays = shifted ? CalorieShiftCalculator.ToFlags(request.CalorieShiftDays) : DaysOfWeek.None;
        }

        // Chỉ đổi shift: KHÔNG tính lại target tuần (TDEE/cân mới sẽ làm nhảy target đã chấp nhận); kiểm shift trên tuần hiện hành rồi ghi phiên bản tuần mới (D2: shift đổi từ hôm nay, ngày đã qua giữ snapshot cũ)
        var onlyShift = request.CalorieShiftMode.HasValue && !request.GoalType.HasValue && !request.WeeklyWeightChangeTargetPercent.HasValue && !request.DietStyle.HasValue
            && !request.ProteinLevel.HasValue && !request.FixedProteinGrams.HasValue && !request.CalorieFloorKcal.HasValue;
        if (onlyShift)
        {
            var current = program.Weeks.Where(w => w.EffectiveFrom <= today).OrderByDescending(w => w.EffectiveFrom).FirstOrDefault()
                ?? throw new BadRequestException(ExceptionMessages.ProgramHasNoWeekTarget);
            ProgramWeekBuilder.UpsertWeek(program, today, new MacroTargets(current.CaloriesTarget, current.ProteinTarget, current.CarbsTarget, current.FatTarget), AdjustmentReason.Manual);   // kiểm khả thi (400) TRƯỚC mọi ghi
            await _uow.EnsureSaveAsync(ct);
            return Unit.Value;
        }

        var latestWeight = await _uow.Repository<WeightEntry>().Query()
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.LogDate)
            .Select(w => (decimal?)(w.TrendWeightKg ?? w.WeightKg))
            .FirstOrDefaultAsync(ct)
            ?? throw new BadRequestException(ExceptionMessages.WeightRequired);

        if (request.WeeklyWeightChangeTargetPercent.HasValue || request.GoalType.HasValue)
            ProgramGoalUpdate.Resign(program, request.WeeklyWeightChangeTargetPercent, latestWeight);

        if (program.ProgramType != ProgramType.Manual)
        {
            var tdee = await _uow.Repository<ExpenditureSnapshot>().Query()
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SnapshotDate)
                .Select(s => (int?)s.EstimatedTdeeKcal)
                .FirstOrDefaultAsync(ct)
                ?? program.ManualInitialTdeeKcal
                ?? throw new BadRequestException(ExceptionMessages.NoExpenditureEstimate);

            var targets = ProgramWeekBuilder.Compute(program, latestWeight, tdee, _options.Value);
            ProgramWeekBuilder.UpsertWeek(program, today, targets, AdjustmentReason.Manual);
        }

        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
