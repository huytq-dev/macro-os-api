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
        bool IsIncomplete = false,
        bool IsFasting = false)
    {
        /// <summary>
        /// kcal đưa vào thuật toán (Expenditure, Holding, check-in, Energy Balance); null = KHÔNG phải ngày calo hợp lệ: user đánh dấu chưa đủ, hoặc có entry nhưng tổng 0 kcal
        /// mà không phải nhịn ăn (vd. chỉ log nước / cà phê đen). Nhịn ăn hợp lệ vẫn là 0 kcal. Chỉ là phép đọc: không đổi IsIncomplete/IsFasting hay dữ liệu đã lưu.
        /// </summary>
        public decimal? AlgorithmKcal => IsIncomplete ? null : IsFasting || TotalCalories > 0 ? TotalCalories : null;
    }

    /// <summary><see cref="LoggedDay.AlgorithmKcal"/> của ngày; ngày vắng trong từ điển (chưa log) = null.</summary>
    public static decimal? AlgorithmKcalOn(IReadOnlyDictionary<DateOnly, LoggedDay> logs, DateOnly date)
        => logs.TryGetValue(date, out var l) ? l.AlgorithmKcal : null;

    /// <summary>
    /// Tổng các ngày đã log (có ≥ 1 entry chưa xoá) trong khoảng; ngày không có entry vắng mặt trong từ điển (= chưa log).
    /// <paramref name="includeFasting"/> = true: ngày user đánh dấu nhịn ăn (IsFasting, không có entry) cũng tính là đã log với 0 kcal — expenditure, check-in, dải ngày và dashboard dùng chế độ này;
    /// thống kê dinh dưỡng (không có dữ liệu chất cho ngày nhịn ăn) giữ mặc định false.
    /// </summary>
    public static async Task<Dictionary<DateOnly, LoggedDay>> LoadDaysAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, CancellationToken ct, bool includeFasting = false)
    {
        var rows = await uow.Repository<FoodLog>().Query()
            .Where(l => l.UserId == userId && l.LogDate >= from && l.LogDate <= to && (l.Entries.Any() || (includeFasting && l.IsFasting)))
            .Select(l => new
            {
                l.LogDate, l.TotalCalories, l.TotalProtein, l.TotalCarbs, l.TotalFat, l.TotalFiber, l.TotalWater, l.IsIncomplete,
                Fasting = l.IsFasting && !l.Entries.Any(),
            })
            .ToListAsync(ct);

        return rows.ToDictionary(r => r.LogDate, r => new LoggedDay(
            r.LogDate, r.Fasting ? 0 : r.TotalCalories, r.Fasting ? 0 : r.TotalProtein, r.Fasting ? 0 : r.TotalCarbs, r.Fasting ? 0 : r.TotalFat, r.Fasting ? 0 : r.TotalFiber, r.TotalWater,
            r.IsIncomplete, r.Fasting));
    }

    /// <summary>FoodLog của ngày (tracking + Entries); tạo mới nếu chưa có.</summary>
    /// <summary>
    /// Thêm entry MỚI (Id gán sẵn) vào ngày log: thêm qua repository (trạng thái Added), KHÔNG dùng <c>log.Entries.Add(entry)</c>:
    /// khi FoodLog đã được nạp và đang tracked (ngày đã có entry), EF coi entry có khoá gán sẵn là đã tồn tại (Modified) nên UPDATE 0 dòng
    /// và <c>DbUpdateConcurrencyException</c> (POST /food-logs/entries lần thứ hai trong ngày trả 500). EF tự gắn entry vào <c>log.Entries</c> nhờ FoodLogId.
    /// </summary>
    public static async Task AddEntryAsync(IUnitOfWork uow, FoodLog log, FoodLogEntry entry, CancellationToken ct)
    {
        entry.FoodLogId = log.Id;
        log.IsFasting = false; // có entry thì ngày không còn là ngày nhịn ăn
        await uow.Repository<FoodLogEntry>().AddAsync(entry, ct);
    }

    /// <summary>Như <see cref="GetOrCreateAsync"/> cho nhiều ngày với MỘT truy vấn (DB ở xa: mỗi truy vấn là một vòng đi về mạng); ngày chưa có thì tạo mới.</summary>
    public static async Task<Dictionary<DateOnly, FoodLog>> GetOrCreateManyAsync(IUnitOfWork uow, Guid userId, IReadOnlyCollection<DateOnly> dates, CancellationToken ct)
    {
        var logs = (await uow.Repository<FoodLog>().Query(tracking: true)
            .Include(l => l.Entries)
            .Where(l => l.UserId == userId && dates.Contains(l.LogDate))
            .ToListAsync(ct)).ToDictionary(l => l.LogDate);
        foreach (var date in dates)
        {
            if (logs.ContainsKey(date)) continue;
            var log = new FoodLog { Id = Guid.NewGuid(), UserId = userId, LogDate = date };
            await uow.Repository<FoodLog>().AddAsync(log, ct);
            logs[date] = log;
        }
        return logs;
    }

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

    /// <summary>Target hiệu lực của ngày: tuần program hiệu lực tại đúng ngày đó, kể cả program đã kết thúc (<see cref="ProgramHistory"/>), đã áp dịch kcal theo ngày của program kiểu Shift; null nếu chưa có program nào phủ ngày đó.</summary>
    public static Task<ProgramWeekModel?> TargetForDateAsync(IUnitOfWork uow, Guid userId, DateOnly date, CancellationToken ct)
        => ProgramHistory.TargetOnAsync(uow, userId, date, ct);

    /// <summary>Ngày + giờ hiện tại theo múi giờ của user (UserProfile.TimeZone; thiếu hoặc sai → UTC), bỏ giây.</summary>
    public static async Task<(DateOnly Date, TimeOnly Time)> LocalNowAsync(IUnitOfWork uow, Guid userId, TimeProvider tp, CancellationToken ct)
    {
        var locale = await UserLocale.GetAsync(uow, userId, tp, ct);
        return (locale.Today, locale.Time);
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
        var programs = await ProgramHistory.LoadAsync(uow, userId, days.Min(), days.Max(), ct);

        var result = new List<ProgramWeekModel>(days.Count);
        foreach (var day in days)
            if (ProgramHistory.TargetOn(programs, day) is { } target) result.Add(target);
        return result;
    }

    /// <summary>
    /// Sau khi log đổi: lưu, ghi lại NutritionDayAggregate của các ngày bị ảnh hưởng (mặc định chỉ <paramref name="changedDate"/>),
    /// dựng lại snapshot từ ngày đổi sớm nhất đến hôm nay (UC-F03), lưu lần nữa.
    /// </summary>
    public static async Task SaveAndRebuildAsync(IUnitOfWork uow, Guid userId, DateOnly changedDate, AlgorithmOptions opt, TimeProvider tp, CancellationToken ct,
        IReadOnlyCollection<DateOnly>? affectedDates = null)
    {
        await uow.EnsureSaveAsync(ct);
        var dates = affectedDates is { Count: > 0 } ? affectedDates : [changedDate];
        // Dựng ĐỒNG BỘ phần gần (hội tụ + trần số dòng) trước khi trả về để refetch thấy số mới; phần còn lại/lỗi thành job bền (xem DerivedDataSync)
        await DerivedDataSync.RunAsync(uow, userId, dates.Min(), dates.Max(), dates, RecalculationCause.FoodLogChanged, opt, tp, ct);
    }
}
