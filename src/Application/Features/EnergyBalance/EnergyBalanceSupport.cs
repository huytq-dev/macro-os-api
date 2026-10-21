namespace Application;

public static class EnergyBalanceSupport
{
    public const int MaxRangeDays = 3660;

    /// <summary>
    /// Các ngày liên tục [from, to] của Energy Balance với MỘT lượt truy vấn cố định (không N+1): program + tuần + dạng ngày, FoodLog của khoảng (gồm nhịn ăn), snapshot expenditure.
    /// <paramref name="from"/> null = ngày có dữ liệu đầu tiên (log đầu tiên hoặc ngày đầu của chuỗi expenditure). Target của MỖI NGÀY lấy từ tuần program hiệu lực tại ngày đó.
    /// </summary>
    public static async Task<(DateOnly From, List<EnergyBalanceDay> Days)> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly? from, DateOnly to, AlgorithmOptions opt, CancellationToken ct)
    {
        if (from is { } f && to.DayNumber - f.DayNumber + 1 > MaxRangeDays) throw new BadRequestException($"Range must not exceed {MaxRangeDays} days.");

        var program = await uow.Repository<MacroProgram>().Query()
            .Include(p => p.Weeks).Include(p => p.DayTargets)
            .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct);

        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, from ?? DateOnly.MinValue, to, ct, includeFasting: true);
        var raw = await ExpenditureSeriesSupport.BuildAsync(uow, userId, to, program?.ExpenditureStartDate, opt, ct);

        var first = from ?? new[] { logs.Count > 0 ? logs.Keys.Min() : to, raw.Count > 0 ? raw[0].Date : to }.Min();
        var expenditure = ExpenditureSeries.ToDays(raw, opt.KcalPerKg, opt.FluxTrendErrorKg).ToDictionary(d => d.Date, d => d.ExpenditureKcal);
        return (first, EnergyBalanceCalculator.Days(first, to, logs, expenditure, TargetFn(program)));
    }

    /// <summary>Target kcal của một ngày: tuần program hiệu lực tại ngày đó (EffectiveFrom ≤ ngày, mới nhất) + dạng ngày trong tuần; null trước tuần đầu / chưa có program. Cache theo (tuần, thứ).</summary>
    public static Func<DateOnly, decimal?> TargetFn(MacroProgram? program) => TargetFn(program, w => w.CaloriesTarget);

    /// <summary>Như <see cref="TargetFn(MacroProgram?)"/> nhưng chọn chỉ tiêu bằng <paramref name="pick"/> (protein/carbs/fat của trang chi tiết nutrient).</summary>
    public static Func<DateOnly, decimal?> TargetFn(MacroProgram? program, Func<ProgramWeekModel, decimal> pick)
    {
        if (program is null) return _ => null;
        var weeks = program.Weeks.OrderByDescending(w => w.EffectiveFrom).ToList();
        var cache = new Dictionary<(Guid, DayOfWeek), decimal>();
        return d =>
        {
            var week = weeks.FirstOrDefault(w => w.EffectiveFrom <= d);
            if (week is null) return null;
            var key = (week.Id, d.DayOfWeek);
            if (!cache.TryGetValue(key, out var kcal)) cache[key] = kcal = pick(ProgramTargetResolver.ForDay(week, program, d.DayOfWeek));
            return kcal;
        };
    }
}
