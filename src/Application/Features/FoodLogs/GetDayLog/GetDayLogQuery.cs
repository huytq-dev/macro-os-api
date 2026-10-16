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

        // Chỉ đọc cột cần dùng (không Include cả FoodItem/Recipe/FoodServing): entries được project thẳng sang model.
        var header = await _uow.Repository<FoodLog>().Query()
            .Where(l => l.UserId == userId && l.LogDate == date)
            .Select(l => new
            {
                l.Id, l.Notes, l.TotalCalories, l.TotalProtein, l.TotalCarbs, l.TotalFat, l.TotalFiber,
            })
            .FirstOrDefaultAsync(ct);

        var targetRow = await _uow.Repository<MacroProgramWeek>().Query()
            .Where(w => w.MacroProgram.UserId == userId && w.MacroProgram.IsActive && w.EffectiveFrom <= date)
            .OrderByDescending(w => w.EffectiveFrom)
            .Select(w => new
            {
                Week = w,
                w.MacroProgram.CalorieShiftMode,
                w.MacroProgram.CalorieShiftPercent,
                w.MacroProgram.CalorieShiftDays,
            })
            .FirstOrDefaultAsync(ct);
        // Program kiểu Shift: kcal của ngày này lệch so với target tuần (tổng tuần giữ nguyên)
        var target = targetRow is null
            ? null
            : CalorieShiftCalculator.ForDate(targetRow.Week.Adapt<ProgramWeekModel>(), targetRow.CalorieShiftMode, targetRow.CalorieShiftPercent, targetRow.CalorieShiftDays, date);

        var rows = header is null
            ? []
            : await _uow.Repository<FoodLogEntry>().Query()
                .Where(e => e.FoodLogId == header.Id)
                .OrderBy(e => e.EatenAt).ThenBy(e => e.CreatedAt)
                .ProjectToType<FoodLogEntryModel>()
                .ToListAsync(ct);

        // MealType lưu dạng chuỗi nên sắp theo thứ tự enum ở bộ nhớ (sort ổn định giữ EatenAt/CreatedAt).
        var entries = rows.OrderBy(e => Enum.Parse<MealType>(e.MealType)).ToList();

        var totals = header is null
            ? new MacroTotals(0, 0, 0, 0, 0)
            : new MacroTotals(header.TotalCalories, header.TotalProtein, header.TotalCarbs, header.TotalFat, header.TotalFiber);

        return new DayLogResponse(date, header?.Notes, totals, target, entries);
    }
}
