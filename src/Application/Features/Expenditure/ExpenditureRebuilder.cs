namespace Application;

/// <summary>
/// Dựng lại DailyProgressSnapshot + ExpenditureSnapshot từ <c>fromDate</c> đến <c>toDate</c> (UC-F01/F02/F03).
/// Gọi sau khi SaveChanges dữ liệu nguồn (FoodLog/WeightEntry); caller SaveChanges lần nữa. Phải chạy dưới khoá dựng lại của user (<see cref="DerivedDataSync"/>)
/// để mọi lần dựng đọc nguồn SAU khi giữ khoá: lần chạy cũ đến trễ vẫn tính từ dữ liệu mới nhất nên không đè kết quả mới.
/// </summary>
public static class ExpenditureRebuilder
{
    // ponytail: load cả khoảng vào RAM, O(ngày × W); đủ cho app cá nhân (W=21, vài năm dữ liệu)
    public static async Task<ExpenditureRebuildResult> RebuildAsync(
        IUnitOfWork uow, Guid userId, DateOnly fromDate, DateOnly toDate, AlgorithmOptions opt, CancellationToken ct, ExpenditureRebuildLimits? limits = null, DateOnly? localToday = null)
    {
        // Ngày "hôm nay" theo múi giờ user: ngày này chưa log hợp lệ thì chưa tính là thiếu (xem ExpenditureStatusMachine). Mặc định = toDate (caller truyền ngày local).
        var today = localToday ?? toDate;
        var program = await uow.Repository<MacroProgram>().Query()
            .Include(p => p.Weeks).ThenInclude(w => w.DayTargetSnapshots)
            .Include(p => p.DayTargets)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct);
        if (program is null) return new(null, 0, false); // chưa có program → không có gì để ước lượng

        var start = program.ExpenditureStartDate;
        if (fromDate < start) fromDate = start;
        if (fromDate > toDate) return new(null, 0, false);

        var w = opt.ExpenditureWindowDays;
        var loadFrom = fromDate.AddDays(-w);

        // Chỉ ngày có ≥ 1 entry chưa xoá mới có trong từ điển (dòng FoodLog rỗng sau khi xoá entry cuối = chưa log)
        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, loadFrom, toDate, ct, includeFasting: true);
        var weights = await uow.Repository<WeightEntry>().Query()
            .Where(x => x.UserId == userId && x.LogDate >= loadFrom && x.LogDate <= toDate)
            .ToDictionaryAsync(x => x.LogDate, ct);

        var days = Enumerable.Range(0, toDate.DayNumber - loadFrom.DayNumber + 1)
            .Select(i => loadFrom.AddDays(i))
            .Select(d => new ExpenditureDay(d,
                // Ngày "log chưa đủ" hoặc có entry nhưng 0 kcal (không nhịn ăn) bị bỏ qua như ngày chưa log: ComputeRaw và Status chỉ đếm ngày có KcalIn
                FoodLogSupport.AlgorithmKcalOn(logs, d),
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
        if (prevTdee is null) return new(null, 0, false); // không có TDEE₀ → bỏ qua

        // Trạng thái ngày trước (máy trạng thái có trạng thái, không cần schema): status của snapshot gần nhất trước fromDate; không có (chuỗi mới) = null ⇒ Calibrating
        ExpenditureStatus? prevStatus = await uow.Repository<ExpenditureSnapshot>().Query()
            .Where(s => s.UserId == userId && s.SnapshotDate >= start && s.SnapshotDate < fromDate)
            .OrderByDescending(s => s.SnapshotDate)
            .Select(s => (ExpenditureStatus?)s.Status)
            .FirstOrDefaultAsync(ct);

        var weeks = program.Weeks.OrderByDescending(x => x.EffectiveFrom).ToList();
        var version = AlgorithmVersions.ExpenditureFor(opt);
        var rows = 0;
        var equalStreak = 0;
        var budget = limits?.RowBudget ?? int.MaxValue;

        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            var i = d.DayNumber - loadFrom.DayNumber;
            var week = weeks.FirstOrDefault(x => x.EffectiveFrom <= d);

            expSnaps.TryGetValue(d, out var snap);
            // Snapshot đã do phiên bản thuật toán MỚI HƠN dựng: không ghi đè bằng kết quả phiên bản cũ; chuỗi TDEE đi tiếp từ giá trị của nó
            if (snap is not null && AlgorithmVersions.IsNewer(snap.AlgorithmVersion, version))
            {
                prevTdee = snap.EstimatedTdeeKcal;
                prevStatus = snap.Status;
                continue;
            }

            // --- DailyProgressSnapshot (UC-F01) ---
            logs.TryGetValue(d, out var log);
            var isLogged = log is not null;
            var consumed = log?.TotalCalories ?? 0;
            var protein = log?.TotalProtein ?? 0;
            var carbs = log?.TotalCarbs ?? 0;
            var fat = log?.TotalFat ?? 0;
            var water = log?.TotalWater ?? 0;
            var target = week is null ? 0 : ProgramTargetResolver.ForDate(week, program, d).CaloriesTarget;
            var weekId = week?.Id;
            var trend = days[i].TrendKg;

            dailySnaps.TryGetValue(d, out var daily);
            var dailyChanged = daily is null || daily.IsLogged != isLogged || daily.CaloriesConsumed != consumed || daily.ProteinConsumed != protein || daily.CarbsConsumed != carbs
                || daily.FatConsumed != fat || daily.WaterConsumed != water || daily.CaloriesTarget != target || daily.MacroProgramWeekId != weekId || daily.TrendWeightKg != trend;
            if (dailyChanged)
            {
                if (daily is null)
                {
                    daily = new DailyProgressSnapshot { Id = Guid.NewGuid(), UserId = userId, SnapshotDate = d };
                    await uow.Repository<DailyProgressSnapshot>().AddAsync(daily, ct);
                    dailySnaps[d] = daily;
                }
                daily.IsLogged = isLogged;
                daily.CaloriesConsumed = consumed;
                daily.ProteinConsumed = protein;
                daily.CarbsConsumed = carbs;
                daily.FatConsumed = fat;
                daily.WaterConsumed = water;
                daily.CaloriesTarget = target;       // cột còn ghi nhưng không còn đọc (target tính khi đọc, xem DailyProgressTargets)
                daily.MacroProgramWeekId = weekId;
                daily.TrendWeightKg = trend;
                rows++;
            }

            // --- ExpenditureSnapshot (UC-F02, ST-01) ---
            var window = days.Skip(Math.Max(0, i - w + 1)).Take(Math.Min(w, i + 1)).Where(x => x.Date >= start).ToList();
            // Cửa sổ trạng thái 8 ngày kết thúc ở d; ngày trước ngày bắt đầu tính coi như không có dữ liệu (fresh start, không dùng dữ liệu trước Start Date)
            var statusDays = days.Skip(Math.Max(0, i - 7)).Take(Math.Min(8, i + 1))
                .Select(x => x.Date >= start ? x : new ExpenditureDay(x.Date, null, null)).ToList();
            var status = ExpenditureStatusMachine.Next(prevStatus, statusDays, today);
            // Hành vi trước #3 (mặc định): TDEE đóng băng trong giai đoạn Calibrating; tắt cờ = học từ ngày 1 (#3, chờ duyệt)
            if (opt.FreezeTdeeDuringCalibration && ExpenditureCalculator.IsCalibrating(d, start, opt.CalibratingDays)) status = ExpenditureStatus.Calibrating;
            var raw = status == ExpenditureStatus.Updating ? ExpenditureCalculator.ComputeRaw(window, opt.KcalPerKg) : null;
            if (raw is null && status == ExpenditureStatus.Updating) status = ExpenditureStatus.Holding;
            prevStatus = status;

            var tdee = raw is null ? prevTdee.Value : ExpenditureCalculator.Smooth(prevTdee.Value, raw.TdeeRaw, opt.SmoothingBeta);
            var avg = raw?.AvgKcalIn ?? Math.Round(window.Where(x => x.KcalIn.HasValue).Select(x => x.KcalIn!.Value).DefaultIfEmpty(0).Average(), 2);
            var rate = raw?.RateKgPerWeek;

            var valuesChanged = snap is null || snap.EstimatedTdeeKcal != tdee || snap.Status != status || snap.AvgCaloriesConsumed != avg || snap.WeightChangeRateKgPerWeek != rate || snap.WindowDays != window.Count;
            if (valuesChanged || snap!.AlgorithmVersion != version)
            {
                if (snap is null)
                {
                    snap = new ExpenditureSnapshot { Id = Guid.NewGuid(), UserId = userId, SnapshotDate = d };
                    await uow.Repository<ExpenditureSnapshot>().AddAsync(snap, ct);
                    expSnaps[d] = snap;
                }
                snap.EstimatedTdeeKcal = tdee;
                snap.Status = status;
                snap.AvgCaloriesConsumed = avg;
                snap.WeightChangeRateKgPerWeek = rate;
                snap.WindowDays = window.Count;
                snap.AlgorithmVersion = version;     // chỉ dòng thực sự được tính lại mới được gắn nhãn; dòng cũ không dựng lại giữ Legacy
                rows++;
            }

            prevTdee = tdee;

            // Hội tụ: kể từ ngày cửa sổ không còn chứa dữ liệu vừa sửa, snapshot trùng bản đang lưu suốt một cửa sổ liên tiếp thì phần đuôi không còn gì đổi
            var inTail = limits?.ConvergeAfter is { } after && d >= after;
            equalStreak = inTail && !dailyChanged && !valuesChanged ? equalStreak + 1 : 0;
            if (equalStreak >= w)
                return new(null, rows, true);
            if (rows >= budget && d < toDate)
                return new(d.AddDays(1), rows, false);
        }
        return new(null, rows, false);
    }
}
