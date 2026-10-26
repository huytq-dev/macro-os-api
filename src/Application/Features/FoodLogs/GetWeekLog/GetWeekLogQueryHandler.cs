namespace Application;

public sealed class GetWeekLogQueryHandler(IUserClock _clock, IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetWeekLogQuery, WeekLogResponse>
{
    public async Task<WeekLogResponse> Handle(GetWeekLogQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await _clock.GetAsync(ct)).Today;   // ngày local của user (quyết định CURRENT_ACTIVE / PLANNED / lịch sử cho từng ngày của tuần)
        var date = request.Date ?? today;
        var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        var sunday = monday.AddDays(6);

        var logs = await FoodLogSupport.LoadDaysAsync(_uow, userId, monday, sunday, ct, includeFasting: true);

        var programs = await ProgramHistory.LoadAsync(_uow, userId, monday, sunday, ct);

        var days = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = monday.AddDays(offset);
            logs.TryGetValue(day, out var log);
            // Totals = tổng RAW của món Food Log; Effective = dinh dưỡng hiệu lực của ngày (Manual → Food Log → Fasting), null khi chưa log
            var totals = log is null
                ? new MacroTotals(0, 0, 0, 0, 0)
                : new MacroTotals(log.FoodCalories, log.FoodProtein, log.FoodCarbs, log.FoodFat, log.FoodFiber);
            return new WeekDayModel(day, log is not null, totals, ProgramHistory.TargetOn(programs, day, today), log?.ToModel());
        }).ToList();

        return new WeekLogResponse(days);
    }
}
