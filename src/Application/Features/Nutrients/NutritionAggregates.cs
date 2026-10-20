using System.Text.Json;

namespace Application;

/// <summary>
/// NutritionDayAggregate: tổng mọi chất của một ngày (macro/fiber/sodium từ cột entry, chất khác từ FoodLogEntryNutrient, water_g = nước trong món + WaterLog).
/// Ghi lại mỗi khi entry/nước đổi (cùng chỗ <see cref="FoodLogSupport.SaveAndRebuildAsync"/>); Overview kỳ dài đọc bảng này thay vì quét entry.
/// </summary>
public static class NutritionAggregates
{
    /// <summary>Tính lại aggregate cho các ngày (ba truy vấn theo khoảng + một truy vấn dòng cũ); ngày không còn entry thì xoá dòng. Chưa lưu.</summary>
    public static async Task RefreshAsync(IUnitOfWork uow, Guid userId, IReadOnlyCollection<DateOnly> dates, TimeProvider tp, CancellationToken ct)
    {
        if (dates.Count == 0) return;
        var set = dates.ToHashSet();
        var from = set.Min();
        var to = set.Max();

        var cols = (await uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodLog.UserId == userId && e.FoodLog.LogDate >= from && e.FoodLog.LogDate <= to)
            .GroupBy(e => e.FoodLog.LogDate)
            .Select(g => new
            {
                Date = g.Key,
                Calories = g.Sum(e => e.Calories), Protein = g.Sum(e => e.Protein), Carbs = g.Sum(e => e.Carbs), Fat = g.Sum(e => e.Fat),
                Fiber = g.Sum(e => e.Fiber ?? 0), Sodium = g.Sum(e => e.Sodium ?? 0),
                FiberKnown = g.Any(e => e.Fiber != null), SodiumKnown = g.Any(e => e.Sodium != null),
            })
            .ToListAsync(ct)).Where(c => set.Contains(c.Date)).ToDictionary(c => c.Date);

        var others = (await uow.Repository<FoodLogEntryNutrient>().Query()
            .Where(n => n.FoodLogEntry.FoodLog.UserId == userId && n.FoodLogEntry.FoodLog.LogDate >= from && n.FoodLogEntry.FoodLog.LogDate <= to && !n.FoodLogEntry.IsDeleted)
            .GroupBy(n => new { n.FoodLogEntry.FoodLog.LogDate, n.NutrientDefinitionId })
            .Select(g => new { g.Key.LogDate, g.Key.NutrientDefinitionId, Sum = g.Sum(n => n.Amount) })
            .ToListAsync(ct)).Where(r => set.Contains(r.LogDate)).ToList();

        var water = (await uow.Repository<FoodLog>().Query()
            .Where(l => l.UserId == userId && l.LogDate >= from && l.LogDate <= to && l.TotalWater > 0)
            .Select(l => new { l.LogDate, l.TotalWater })
            .ToListAsync(ct)).ToDictionary(w => w.LogDate, w => w.TotalWater);

        var codeById = (await uow.Repository<NutrientDefinition>().Query().Select(d => new { d.Id, d.Code }).ToListAsync(ct))
            .ToDictionary(d => d.Id, d => d.Code);

        var repo = uow.Repository<NutritionDayAggregate>();
        var existing = (await repo.GetAllTrackedAsync(a => a.UserId == userId && a.LocalDate >= from && a.LocalDate <= to, ct))
            .Where(a => set.Contains(a.LocalDate)).ToDictionary(a => a.LocalDate);
        var now = tp.GetUtcNow();

        foreach (var date in set)
        {
            existing.TryGetValue(date, out var row);
            if (!cols.TryGetValue(date, out var c))
            {
                if (row is not null) await repo.RemoveAsync(row, ct);
                continue;
            }

            var amounts = new Dictionary<string, decimal>
            {
                ["energy_kcal"] = c.Calories, ["protein_g"] = c.Protein, ["carbs_g"] = c.Carbs, ["fat_g"] = c.Fat,
            };
            // Ngày không entry nào có fiber/sodium: vắng mặt trong JSON (= chưa có dữ liệu), không ghi 0
            if (c.FiberKnown) amounts["fiber_g"] = c.Fiber;
            if (c.SodiumKnown) amounts["sodium_mg"] = c.Sodium;
            foreach (var r in others.Where(r => r.LogDate == date && r.Sum is not null))
                if (codeById.TryGetValue(r.NutrientDefinitionId, out var code)) amounts[code] = NutritionRounding.Round(r.Sum!.Value, NutritionRounding.Micro);
            if (water.TryGetValue(date, out var ml)) amounts["water_g"] = amounts.GetValueOrDefault("water_g") + ml;

            if (row is null)
            {
                row = new NutritionDayAggregate { Id = Guid.NewGuid(), UserId = userId, LocalDate = date };
                await repo.AddAsync(row, ct);
            }
            row.Calories = c.Calories; row.Protein = c.Protein; row.Carbs = c.Carbs; row.Fat = c.Fat; row.Fiber = c.Fiber;
            row.NutrientsJson = JsonSerializer.Serialize(amounts);
            row.LastCalculatedAt = now;
        }
    }

    /// <summary>
    /// Tổng từng chất (theo mã) trong [from, to] từ aggregate; chất không xuất hiện ngày nào → vắng mặt (= chưa có dữ liệu, khác 0).
    /// Tự chữa: ngày đã log mà chưa có aggregate (dữ liệu trước WP5) được dựng và lưu ngay lần đọc đầu.
    /// </summary>
    public static async Task<Dictionary<string, decimal?>> SumsAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to,
        IReadOnlyCollection<DateOnly> loggedDates, TimeProvider tp, CancellationToken ct)
    {
        var rows = await LoadAsync(uow, userId, from, to, ct);
        var missing = loggedDates.Except(rows.Select(r => r.LocalDate)).ToList();
        if (missing.Count > 0)
        {
            await RefreshAsync(uow, userId, missing, tp, ct);
            await uow.SaveChangesAsync(ct);
            rows = await LoadAsync(uow, userId, from, to, ct);
        }

        var sums = new Dictionary<string, decimal?>();
        foreach (var r in rows.Where(r => r.NutrientsJson is not null))
            foreach (var (code, amount) in JsonSerializer.Deserialize<Dictionary<string, decimal>>(r.NutrientsJson!)!)
                sums[code] = (sums.GetValueOrDefault(code) ?? 0) + amount;
        return sums;
    }

    private static async Task<List<(DateOnly LocalDate, string? NutrientsJson)>> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly from, DateOnly to, CancellationToken ct)
        => (await uow.Repository<NutritionDayAggregate>().Query()
            .Where(a => a.UserId == userId && a.LocalDate >= from && a.LocalDate <= to)
            .Select(a => new { a.LocalDate, a.NutrientsJson })
            .ToListAsync(ct)).Select(a => (a.LocalDate, a.NutrientsJson)).ToList();
}
