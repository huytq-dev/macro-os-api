namespace Application;

/// <summary>
/// Dựng lại DailyProgressSnapshot + ExpenditureSnapshot từ <c>fromDate</c> đến <c>toDate</c> (UC-F01/F02/F03).
/// Gọi sau khi SaveChanges dữ liệu nguồn (FoodLog/WeightEntry); caller SaveChanges lần nữa.
/// </summary>
public static class ExpenditureRebuilder
{
    // ponytail: load cả khoảng vào RAM, O(ngày × W); đủ cho app cá nhân (W=21, vài năm dữ liệu)
    public static async Task RebuildAsync(IUnitOfWork uow, Guid userId, DateOnly fromDate, DateOnly toDate, AlgorithmOptions opt, CancellationToken ct)
    {
        var program = await uow.Repository<MacroProgram>().Query()
            .Include(p => p.Weeks)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct);
        if (program is null) return; // chưa có program → không có gì để ước lượng

        var start = program.ExpenditureStartDate;
        if (fromDate < start) fromDate = start;
        if (fromDate > toDate) return;

        var w = opt.ExpenditureWindowDays;
        var loadFrom = fromDate.AddDays(-w);

        var logs = await uow.Repository<FoodLog>().Query()
            .Where(l => l.UserId == userId && l.LogDate >= loadFrom && l.LogDate <= toDate)
            .ToDictionaryAsync(l => l.LogDate, ct);
        var weights = await uow.Repository<WeightEntry>().Query()
            .Where(x => x.UserId == userId && x.LogDate >= loadFrom && x.LogDate <= toDate)
            .ToDictionaryAsync(x => x.LogDate, ct);

        var days = Enumerable.Range(0, toDate.DayNumber - loadFrom.DayNumber + 1)
            .Select(i => loadFrom.AddDays(i))
            .Select(d => new ExpenditureDay(d,
                logs.TryGetValue(d, out var l) ? l.TotalCalories : null,
                weights.TryGetValue(d, out var we) ? we.TrendWeightKg : null))
            .ToList();

        var expSnaps = (await uow.Repository<ExpenditureSnapshot>().GetAllTrackedAsync(s => s.UserId == userId && s.SnapshotDate >= fromDate && s.SnapshotDate <= toDate, ct))
            .ToDictionary(s => s.SnapshotDate);
        var dailySnaps = (await uow.Repository<DailyProgressSnapshot>().GetAllTrackedAsync(s => s.UserId == userId && s.SnapshotDate >= fromDate && s.SnapshotDate <= toDate, ct))
            .ToDictionary(s => s.SnapshotDate);

        // TDEE[t−1]: snapshot gần nhất trước fromDate (≥ start), hoặc chính snapshot start (TDEE₀), hoặc TDEE nhập tay
        var prevTdee = await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= start && s.SnapshotDate < fromDate)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => (decimal?)s.EstimatedTdeeKcal)
            .FirstOrDefaultAsync(ct)
            ?? (expSnaps.TryGetValue(start, out var s0) ? s0.EstimatedTdeeKcal : (decimal?)null)
            ?? program.ManualInitialTdeeKcal;
        if (prevTdee is null) return; // không có TDEE₀ → bỏ qua

        var weeks = program.Weeks.OrderByDescending(x => x.EffectiveFrom).ToList();

        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            var i = d.DayNumber - loadFrom.DayNumber;
            var week = weeks.FirstOrDefault(x => x.EffectiveFrom <= d);

            // --- DailyProgressSnapshot (UC-F01) ---
            if (!dailySnaps.TryGetValue(d, out var daily))
            {
                daily = new DailyProgressSnapshot { Id = Guid.NewGuid(), UserId = userId, SnapshotDate = d };
                await uow.Repository<DailyProgressSnapshot>().AddAsync(daily, ct);
                dailySnaps[d] = daily;
            }
            logs.TryGetValue(d, out var log);
            daily.CaloriesConsumed = log?.TotalCalories ?? 0;
            daily.ProteinConsumed  = log?.TotalProtein ?? 0;
            daily.CarbsConsumed    = log?.TotalCarbs ?? 0;
            daily.FatConsumed      = log?.TotalFat ?? 0;
            daily.WaterConsumed    = log?.TotalWater ?? 0;
            daily.CaloriesTarget   = week?.CaloriesTarget ?? 0;
            daily.MacroProgramWeekId = week?.Id;
            daily.TrendWeightKg    = days[i].TrendKg;

            // --- ExpenditureSnapshot (UC-F02, ST-01) ---
            var window = days.Skip(Math.Max(0, i - w + 1)).Take(Math.Min(w, i + 1)).Where(x => x.Date >= start).ToList();
            var last7 = days.Skip(Math.Max(0, i - 6)).Take(Math.Min(7, i + 1)).ToList();
            var status = ExpenditureCalculator.Status(d, start, opt.CalibratingDays, last7);
            var raw = status == ExpenditureStatus.Updating ? ExpenditureCalculator.ComputeRaw(window, opt.KcalPerKg) : null;
            if (raw is null && status == ExpenditureStatus.Updating) status = ExpenditureStatus.Holding;

            var tdee = raw is null ? prevTdee.Value : ExpenditureCalculator.Smooth(prevTdee.Value, raw.TdeeRaw, opt.SmoothingBeta);

            if (!expSnaps.TryGetValue(d, out var snap))
            {
                snap = new ExpenditureSnapshot { Id = Guid.NewGuid(), UserId = userId, SnapshotDate = d };
                await uow.Repository<ExpenditureSnapshot>().AddAsync(snap, ct);
                expSnaps[d] = snap;
            }
            snap.EstimatedTdeeKcal = tdee;
            snap.Status = status;
            snap.AvgCaloriesConsumed = raw?.AvgKcalIn ?? Math.Round(window.Where(x => x.KcalIn.HasValue).Select(x => x.KcalIn!.Value).DefaultIfEmpty(0).Average(), 2);
            snap.WeightChangeRateKgPerWeek = raw?.RateKgPerWeek;
            snap.WindowDays = window.Count;

            prevTdee = tdee;
        }
    }
}
