namespace Application;

/// <summary>Tuần (Thứ 2 → Chủ nhật) chứa ngày `Date`, mặc định hôm nay: tổng macro + target hiệu lực của từng ngày, một request cho dashboard.</summary>
public sealed record GetWeekLogQuery(DateOnly? Date) : IRequest<WeekLogResponse>;

public sealed class GetWeekLogQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetWeekLogQuery, WeekLogResponse>
{
    public async Task<WeekLogResponse> Handle(GetWeekLogQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);
        var monday = date.AddDays(-(((int)date.DayOfWeek + 6) % 7));
        var sunday = monday.AddDays(6);

        var logs = await FoodLogSupport.LoadDaysAsync(_uow, userId, monday, sunday, ct);

        var weeks = await _uow.Repository<MacroProgramWeek>().Query()
            .Include(w => w.MacroProgram)
            .Where(w => w.MacroProgram.UserId == userId && w.MacroProgram.IsActive && w.EffectiveFrom <= sunday)
            .OrderByDescending(w => w.EffectiveFrom)
            .ToListAsync(ct);

        var days = Enumerable.Range(0, 7).Select(offset =>
        {
            var day = monday.AddDays(offset);
            logs.TryGetValue(day, out var log);
            var totals = log is null
                ? new MacroTotals(0, 0, 0, 0, 0)
                : new MacroTotals(log.TotalCalories, log.TotalProtein, log.TotalCarbs, log.TotalFat, log.TotalFiber);
            var effective = weeks.FirstOrDefault(w => w.EffectiveFrom <= day);
            var target = effective is null
                ? null
                : CalorieShiftCalculator.ForDate(
                    effective.Adapt<ProgramWeekModel>(), effective.MacroProgram.CalorieShiftMode,
                    effective.MacroProgram.CalorieShiftPercent, effective.MacroProgram.CalorieShiftDays, day);
            return new WeekDayModel(day, log is not null, totals, target);
        }).ToList();

        return new WeekLogResponse(days);
    }
}
