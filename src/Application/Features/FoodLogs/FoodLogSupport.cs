namespace Application;

/// <summary>Snapshot macro cho entry và tổng ngày (BR-05: entry giữ snapshot, không đọc lại FoodItem khi hiển thị).</summary>
public static class FoodLogSupport
{
    /// <summary>Gram thực ăn: Quantity × serving, hoặc Quantity là gram khi không có serving.</summary>
    public static decimal Grams(decimal quantity, FoodServing? serving) => NutritionCalculator.Grams(quantity, serving?.WeightInGrams);

    /// <param name="profile">Hồ sơ /100 g của món (NutrientSnapshot.LoadProfileAsync): lấy fiber, sodium; thiếu thì để null.</param>
    public static void Snapshot(FoodLogEntry e, FoodItem food, decimal grams, IReadOnlyDictionary<string, NutrientValue>? profile = null)
    {
        var m = NutritionCalculator.SnapshotMacros(food.CaloriesPer100g, food.ProteinPer100g, food.CarbsPer100g, food.FatPer100g, profile, grams);
        e.Calories = m.Calories;
        e.Protein  = m.Protein;
        e.Carbs    = m.Carbs;
        e.Fat      = m.Fat;
        e.Fiber    = m.Fiber;
        e.Sodium   = m.Sodium;
    }

    /// <summary>Tính lại tổng ngày từ entries chưa xoá (log phải Include Entries).</summary>
    public static void RecalcTotals(FoodLog log)
    {
        var totals = NutritionCalculator.SumEntries(log.Entries);
        log.TotalCalories = totals.Calories;
        log.TotalProtein  = totals.Protein;
        log.TotalCarbs    = totals.Carbs;
        log.TotalFat      = totals.Fat;
        log.TotalFiber    = totals.Fiber;
    }

    // Ngày tính là "đã log" khi có ≥ 1 entry chưa xoá (dòng FoodLog rỗng sau khi xoá entry cuối thì không). Dùng chung cho thuật toán, check-in và UI.

    /// <param name="TotalCalories">Tổng ngày; cũng là kcal đưa vào ALG-03 (từ điển chỉ chứa ngày đã log).</param>
    public sealed record LoggedDay(
        DateOnly Date,
        decimal TotalCalories, decimal TotalProtein, decimal TotalCarbs, decimal TotalFat, decimal TotalFiber, decimal TotalWater,
        bool IsIncomplete = false);

    /// <summary>Tổng các ngày đã log (có ≥ 1 entry chưa xoá) trong khoảng; ngày không có entry vắng mặt trong từ điển (= chưa log).</summary>
    public static async Task<Dictionary<DateOnly, LoggedDay>> LoadDaysAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await uow.Repository<FoodLog>().Query()
            .Where(l => l.UserId == userId && l.LogDate >= from && l.LogDate <= to && l.Entries.Any())
            .Select(l => new
            {
                l.LogDate, l.TotalCalories, l.TotalProtein, l.TotalCarbs, l.TotalFat, l.TotalFiber, l.TotalWater, l.IsIncomplete,
            })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.LogDate, r => new LoggedDay(
            r.LogDate, r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.TotalFiber, r.TotalWater, r.IsIncomplete));
    }

    /// <summary>FoodLog của ngày (tracking + Entries); tạo mới nếu chưa có.</summary>
    public static async Task<FoodLog> GetOrCreateAsync(IUnitOfWork uow, Guid userId, DateOnly date, CancellationToken ct)
    {
        var log = await uow.Repository<FoodLog>().Query(tracking: true)
            .Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == date, ct);
        if (log is not null) return log;

        log = new FoodLog { Id = Guid.NewGuid(), UserId = userId, LogDate = date };
        await uow.Repository<FoodLog>().AddAsync(log, ct);
        return log;
    }

    /// <summary>FoodItem user được phép log: hệ thống, hoặc custom/quick-add của chính mình.</summary>
    public static async Task<FoodItem> GetLoggableFoodAsync(IUnitOfWork uow, Guid userId, Guid foodItemId, CancellationToken ct)
    {
        var food = await uow.Repository<FoodItem>().FindAsync(f => f.Id == foodItemId, ct, f => f.Servings);
        if (food is null || (food.CreatedByUserId is { } owner && owner != userId))
            throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", foodItemId));
        return food;
    }

    /// <summary>Entry thuộc user hiện tại (tracking, kèm FoodLog + Entries để tính tổng).</summary>
    public static async Task<FoodLogEntry> GetOwnedEntryAsync(IUnitOfWork uow, Guid userId, Guid entryId, CancellationToken ct)
        => await uow.Repository<FoodLogEntry>().Query(tracking: true)
            .Include(e => e.FoodLog).ThenInclude(l => l.Entries)
            .FirstOrDefaultAsync(e => e.Id == entryId && e.FoodLog.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLogEntry", entryId));

    /// <summary>Target hiệu lực của ngày từ program đang hoạt động (đã áp dịch kcal theo ngày của program kiểu Shift); null nếu chưa có program.</summary>
    public static async Task<ProgramWeekModel?> TargetForDateAsync(IUnitOfWork uow, Guid userId, DateOnly date, CancellationToken ct)
    {
        var week = await uow.Repository<MacroProgramWeek>().Query()
            .Include(w => w.MacroProgram).ThenInclude(p => p.DayTargets)
            .Where(w => w.MacroProgram.UserId == userId && w.MacroProgram.IsActive && w.EffectiveFrom <= date)
            .OrderByDescending(w => w.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        return week is null ? null : ProgramTargetResolver.ForDate(week, week.MacroProgram, date);
    }

    /// <summary>Ngày + giờ hiện tại theo múi giờ của user (UserProfile.TimeZone; thiếu hoặc sai → UTC), bỏ giây.</summary>
    public static async Task<(DateOnly Date, TimeOnly Time)> LocalNowAsync(IUnitOfWork uow, Guid userId, TimeProvider tp, CancellationToken ct)
    {
        var zoneId = await uow.Repository<UserProfile>().Query()
            .Where(p => p.UserId == userId)
            .Select(p => p.TimeZone)
            .FirstOrDefaultAsync(ct);

        var zone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(zoneId))
        {
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        var local = TimeZoneInfo.ConvertTime(tp.GetUtcNow(), zone);
        return (DateOnly.FromDateTime(local.DateTime), new TimeOnly(local.Hour, local.Minute));
    }

    /// <summary>
    /// Giờ ăn + bữa cuối cùng của entry/note. Giờ thiếu: hôm nay (theo múi giờ user) → giờ hiện tại; ngày khác → giờ mặc định của bữa (hoặc 12:00).
    /// Bữa thiếu → suy từ giờ. Chỉ truy vấn múi giờ khi thật sự cần.
    /// </summary>
    public static async Task<(TimeOnly EatenAt, MealType Meal)> ResolveTimeAsync(
        IUnitOfWork uow, Guid userId, DateOnly logDate, TimeOnly? eatenAt, MealType? meal, TimeProvider tp, CancellationToken ct)
        => ResolveTime(logDate, eatenAt, meal, eatenAt is null ? await LocalNowAsync(uow, userId, tp, ct) : null);

    /// <summary>Phần thuần của <see cref="ResolveTimeAsync"/>; <paramref name="now"/> chỉ cần khi <paramref name="eatenAt"/> null (batch nạp một lần cho cả lô).</summary>
    public static (TimeOnly EatenAt, MealType Meal) ResolveTime(DateOnly logDate, TimeOnly? eatenAt, MealType? meal, (DateOnly Date, TimeOnly Time)? now)
    {
        var time = eatenAt
            ?? (now is { } n && logDate == n.Date ? n.Time : meal is { } m ? MealTimes.Default(m) : new TimeOnly(12, 0));
        return (time, meal ?? MealTimes.ForTime(time));
    }

    /// <summary>Target hiệu lực của từng ngày trong <paramref name="days"/> (đã áp dịch kcal theo ngày); bỏ qua ngày chưa có program.</summary>
    public static async Task<List<ProgramWeekModel>> ProgramTargetsAsync(IUnitOfWork uow, Guid userId, IReadOnlyCollection<DateOnly> days, CancellationToken ct)
    {
        if (days.Count == 0) return [];
        var last = days.Max();
        var weeks = await uow.Repository<MacroProgramWeek>().Query()
            .Include(w => w.MacroProgram).ThenInclude(p => p.DayTargets)
            .Where(w => w.MacroProgram.UserId == userId && w.MacroProgram.IsActive && w.EffectiveFrom <= last)
            .OrderByDescending(w => w.EffectiveFrom)
            .ToListAsync(ct);

        var result = new List<ProgramWeekModel>(days.Count);
        foreach (var day in days)
        {
            var week = weeks.FirstOrDefault(w => w.EffectiveFrom <= day);
            if (week is null) continue;
            result.Add(ProgramTargetResolver.ForDate(week, week.MacroProgram, day));
        }
        return result;
    }

    public static DateOnly Today(TimeProvider tp) => DateOnly.FromDateTime(tp.GetUtcNow().UtcDateTime);

    /// <summary>
    /// Sau khi log đổi: lưu, ghi lại NutritionDayAggregate của các ngày bị ảnh hưởng (mặc định chỉ <paramref name="changedDate"/>),
    /// dựng lại snapshot từ ngày đổi sớm nhất đến hôm nay (UC-F03), lưu lần nữa.
    /// </summary>
    public static async Task SaveAndRebuildAsync(IUnitOfWork uow, Guid userId, DateOnly changedDate, AlgorithmOptions opt, TimeProvider tp, CancellationToken ct,
        IReadOnlyCollection<DateOnly>? affectedDates = null)
    {
        await uow.EnsureSaveAsync(ct);
        var dates = affectedDates is { Count: > 0 } ? affectedDates : [changedDate];
        await NutritionAggregates.RefreshAsync(uow, userId, dates, tp, ct);
        await ExpenditureRebuilder.RebuildAsync(uow, userId, dates.Min(), Today(tp), opt, ct);
        await uow.SaveChangesAsync(ct);
    }
}
