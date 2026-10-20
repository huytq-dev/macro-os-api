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
                l.Id, l.TotalCalories, l.TotalProtein, l.TotalCarbs, l.TotalFat, l.TotalFiber, l.TotalWater,
            })
            .FirstOrDefaultAsync(ct);

        var target = await FoodLogSupport.TargetForDateAsync(_uow, userId, date, ct);

        var rows = header is null
            ? []
            : await _uow.Repository<FoodLogEntry>().Query()
                .Where(e => e.FoodLogId == header.Id)
                .OrderBy(e => e.EatenAt).ThenBy(e => e.CreatedAt)
                .ProjectToType<FoodLogEntryModel>()
                .ToListAsync(ct);

        // iconKey hiệu lực: icon riêng của user (một truy vấn cho cả ngày) thắng icon của món
        var overrides = await FoodIconSupport.OverridesAsync(_uow, userId, rows.Where(r => r.FoodItemId is not null).Select(r => r.FoodItemId!.Value), ct);

        // Timeline theo giờ: EatenAt luôn có giá trị (dòng cũ null → giờ mặc định của bữa); sort ổn định giữ thứ tự tạo.
        var entries = rows
            .Select(r => r with
            {
                EatenAt = r.EatenAt ?? MealTimes.Default(Enum.Parse<MealType>(r.MealType)),
                IconKey = FoodIconSupport.Effective(r.FoodItemId, r.IconKey, overrides),
            })
            .OrderBy(e => e.EatenAt)
            .ToList();

        var totals = header is null
            ? new MacroTotals(0, 0, 0, 0, 0)
            : new MacroTotals(header.TotalCalories, header.TotalProtein, header.TotalCarbs, header.TotalFat, header.TotalFiber);

        var notes = await _uow.Repository<FoodLogNote>().Query()
            .Where(n => n.UserId == userId && n.LogDate == date)
            .OrderBy(n => n.NotedAt).ThenBy(n => n.CreatedAt)
            .Select(n => new FoodLogNoteModel(n.Id, n.Text, n.NotedAt, n.CreatedAt, n.Name, n.Color.ToString()))
            .ToListAsync(ct);

        return new DayLogResponse(date, entries.Count > 0, notes, totals, target, entries, header?.TotalWater ?? 0);
    }
}
