namespace Application;

/// <summary>Snapshot macro cho entry và tổng ngày (BR-05: entry giữ snapshot, không đọc lại FoodItem khi hiển thị).</summary>
public static class FoodLogSupport
{
    /// <summary>Gram thực ăn: Quantity × serving, hoặc Quantity là gram khi không có serving.</summary>
    public static decimal Grams(decimal quantity, FoodServing? serving) => quantity * (serving?.WeightInGrams ?? 1m);

    public static void Snapshot(FoodLogEntry e, FoodItem food, decimal grams)
    {
        var k = grams / 100m;
        e.Calories = Math.Round(food.CaloriesPer100g * k, 2);
        e.Protein  = Math.Round(food.ProteinPer100g * k, 2);
        e.Carbs    = Math.Round(food.CarbsPer100g * k, 2);
        e.Fat      = Math.Round(food.FatPer100g * k, 2);
        e.Fiber    = food.FiberPer100g is { } f ? Math.Round(f * k, 2) : null;
        e.Sodium   = food.SodiumPer100g is { } s ? Math.Round(s * k, 2) : null;
    }

    /// <summary>Tính lại tổng ngày từ entries chưa xoá (log phải Include Entries).</summary>
    public static void RecalcTotals(FoodLog log)
    {
        var live = log.Entries.Where(e => !e.IsDeleted).ToList();
        log.TotalCalories = live.Sum(e => e.Calories);
        log.TotalProtein  = live.Sum(e => e.Protein);
        log.TotalCarbs    = live.Sum(e => e.Carbs);
        log.TotalFat      = live.Sum(e => e.Fat);
        log.TotalFiber    = live.Sum(e => e.Fiber ?? 0);
    }

    /// <summary>Ngày tính là "đã log": Fasting (0 kcal thật) hoặc không Partial và có ≥ 1 entry chưa xoá. Dùng chung cho thuật toán, check-in và UI.</summary>
    public static bool IsLogged(FoodLogStatus status, bool hasEntries)
        => status == FoodLogStatus.Fasting || (status != FoodLogStatus.Partial && hasEntries);

    /// <param name="IsLogged">Theo <see cref="IsLogged(FoodLogStatus, bool)"/>.</param>
    /// <param name="TotalCalories">Tổng thật để hiển thị (Partial vẫn hiện số đã ăn).</param>
    public sealed record LoggedDay(
        DateOnly Date, FoodLogStatus Status, bool IsLogged,
        decimal TotalCalories, decimal TotalProtein, decimal TotalCarbs, decimal TotalFat, decimal TotalFiber, decimal TotalWater)
    {
        /// <summary>kcal đưa vào ALG-03: null = chưa log hoặc Partial; Fasting = 0.</summary>
        public decimal? AlgorithmKcal => IsLogged ? TotalCalories : null;
    }

    /// <summary>Tổng + trạng thái các ngày có dòng FoodLog trong khoảng; ngày không có dòng vắng mặt trong từ điển (= chưa log).</summary>
    public static async Task<Dictionary<DateOnly, LoggedDay>> LoadDaysAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
    {
        var rows = await uow.Repository<FoodLog>().Query()
            .Where(l => l.UserId == userId && l.LogDate >= from && l.LogDate <= to)
            .Select(l => new
            {
                l.LogDate, l.Status, l.TotalCalories, l.TotalProtein, l.TotalCarbs, l.TotalFat, l.TotalFiber, l.TotalWater,
                HasEntries = l.Entries.Any(),
            })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.LogDate, r => new LoggedDay(
            r.LogDate, r.Status, IsLogged(r.Status, r.HasEntries),
            r.TotalCalories, r.TotalProtein, r.TotalCarbs, r.TotalFat, r.TotalFiber, r.TotalWater));
    }

    /// <summary>FoodLog của ngày (tracking + Entries); tạo mới nếu chưa có. Mở để ghi entry vào ngày Fasting → ngày có ăn, về Unmarked.</summary>
    public static async Task<FoodLog> GetOrCreateAsync(IUnitOfWork uow, Guid userId, DateOnly date, CancellationToken ct)
    {
        var log = await uow.Repository<FoodLog>().Query(tracking: true)
            .Include(l => l.Entries)
            .FirstOrDefaultAsync(l => l.UserId == userId && l.LogDate == date, ct);
        if (log is not null)
        {
            if (log.Status == FoodLogStatus.Fasting)
            {
                log.Status = FoodLogStatus.Unmarked;
                log.StatusSource = DayStatusSource.System;
            }
            return log;
        }

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
        var row = await uow.Repository<MacroProgramWeek>().Query()
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

        return row is null
            ? null
            : CalorieShiftCalculator.ForDate(row.Week.Adapt<ProgramWeekModel>(), row.CalorieShiftMode, row.CalorieShiftPercent, row.CalorieShiftDays, date);
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
    {
        TimeOnly time;
        if (eatenAt is { } given)
        {
            time = given;
        }
        else
        {
            var now = await LocalNowAsync(uow, userId, tp, ct);
            time = logDate == now.Date ? now.Time : meal is { } m ? MealTimes.Default(m) : new TimeOnly(12, 0);
        }
        return (time, meal ?? MealTimes.ForTime(time));
    }

    public static DateOnly Today(TimeProvider tp) => DateOnly.FromDateTime(tp.GetUtcNow().UtcDateTime);

    /// <summary>Sau khi log đổi: lưu, dựng lại snapshot từ ngày đó đến hôm nay (UC-F03), lưu lần nữa.</summary>
    public static async Task SaveAndRebuildAsync(IUnitOfWork uow, Guid userId, DateOnly changedDate, AlgorithmOptions opt, TimeProvider tp, CancellationToken ct)
    {
        await uow.EnsureSaveAsync(ct);
        await ExpenditureRebuilder.RebuildAsync(uow, userId, changedDate, Today(tp), opt, ct);
        await uow.SaveChangesAsync(ct);
    }
}
