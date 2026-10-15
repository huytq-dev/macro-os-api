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

    public static DateOnly Today(TimeProvider tp) => DateOnly.FromDateTime(tp.GetUtcNow().UtcDateTime);
}
