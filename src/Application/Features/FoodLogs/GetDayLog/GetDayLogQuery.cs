namespace Application;

/// <summary>UC-D01: log của một ngày (mặc định hôm nay) + tổng + target tuần hiệu lực.</summary>
public sealed record GetDayLogQuery(DateOnly? Date) : IRequest<DayLogResponse>;

public sealed class GetDayLogQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetDayLogQuery, DayLogResponse>
{
    public async Task<DayLogResponse> Handle(GetDayLogQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);

        var log = await _uow.Repository<FoodLog>().Query()
            .Include(l => l.Entries).ThenInclude(e => e.FoodItem)
            .Include(l => l.Entries).ThenInclude(e => e.Recipe)
            .Include(l => l.Entries).ThenInclude(e => e.FoodServing)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == date, ct);

        var week = await _uow.Repository<MacroProgramWeek>().Query()
            .Where(w => w.MacroProgram.UserId == userId && w.MacroProgram.IsActive && w.EffectiveFrom <= date)
            .OrderByDescending(w => w.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        var entries = (log?.Entries ?? [])
            .OrderBy(e => e.MealType).ThenBy(e => e.EatenAt).ThenBy(e => e.CreatedAt)
            .Select(e => e.Adapt<FoodLogEntryModel>())
            .ToList();

        var totals = log is null
            ? new MacroTotals(0, 0, 0, 0, 0)
            : new MacroTotals(log.TotalCalories, log.TotalProtein, log.TotalCarbs, log.TotalFat, log.TotalFiber);

        return new DayLogResponse(date, log?.Notes, totals, week?.Adapt<ProgramWeekModel>(), entries);
    }
}
