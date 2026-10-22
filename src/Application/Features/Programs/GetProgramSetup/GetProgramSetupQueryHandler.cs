namespace Application;

public sealed class GetProgramSetupQueryHandler(
    IUnitOfWork _uow, IUserContext _userContext, IOptions<AlgorithmOptions> _options, TimeProvider _timeProvider)
    : IRequestHandler<GetProgramSetupQuery, ProgramSetupResponse>
{
    public async Task<ProgramSetupResponse> Handle(GetProgramSetupQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;   // ngày local của user
        var (profile, weight) = await OnboardingData.LoadAsync(_uow, userId, ct);

        // Goal điền sẵn cho wizard: goal đang hoạt động, hoặc của program gần nhất (kể cả đã kết thúc); chưa từng có thì null và ngày check-in mặc định = hôm nay
        var last = await _uow.Repository<MacroProgram>().Query()
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.IsActive).ThenByDescending(p => p.StartDate)
            .FirstOrDefaultAsync(ct);

        // TDEE: cùng resolver với preview/create (expenditure hiện tại trước, rồi nhập tay khởi điểm, rồi công thức)
        var resolved = await InitialTdeeResolver.ResolveAsync(_uow, userId, profile, weight, last?.ManualInitialTdeeKcal, today, _options.Value, ct);
        var estimate = resolved.Formula;
        var tdee = resolved.TdeeKcal;

        var (standard, low) = ProgramPreviewCalculator.Floors(estimate?.BmrKcal);
        var limits = ProgramDraftCalculator.Limits(tdee, _options.Value);

        // Mặc định = giữ cân (Balanced, protein vừa) ở đúng TDEE hiện tại: điểm xuất phát cho editor/Manual
        var defaults = ProgramPreviewCalculator.Compute(
            profile, weight,
            new ProgramPreviewInput(GoalType.Maintenance, null, 0m, DietStyle.Balanced, ProteinLevel.Moderate, CalorieFloorChoice.Off,
                profile.TrainingType, CalorieShiftMode.Even, null, null, tdee),
            _options.Value.KcalPerKg, today, await ExpenditureSettingsSupport.BmrSexAsync(_uow, userId, ct)).Days
            .Select(d => new ProgramDayModel(d.DayOfWeek, d.Calories, d.ProteinG, d.CarbsG, d.FatG, false))
            .ToList();

        var goalTarget = await _uow.Repository<Goal>().Query()
            .Where(g => g.UserId == userId)
            .OrderByDescending(g => g.Status == GoalStatus.Active).ThenByDescending(g => g.StartDate)
            .Select(g => (decimal?)g.TargetWeightKg)
            .FirstOrDefaultAsync(ct);
        var goal = last is null ? null : new ProgramSetupGoalModel(last.GoalType.ToString(), Math.Abs(last.WeeklyWeightChangeTargetPercent), goalTarget ?? last.TargetWeightKg);

        return new ProgramSetupResponse(
            weight, tdee, new ProgramFloorsModel(standard, low), limits.KcalMax, limits.ProteinPerKgMax, defaults,
            goal, (last?.CheckInWeekday ?? today.DayOfWeek).ToString(), resolved.Source, resolved.IsStale);
    }
}
