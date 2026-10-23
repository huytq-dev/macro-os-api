namespace Application;

public static class EnergyBalanceSupport
{
    public const int MaxRangeDays = 3660;

    /// <summary>
    /// Các ngày liên tục [from, to] của Energy Balance với MỘT lượt truy vấn cố định (không N+1): program + tuần + dạng ngày, FoodLog của khoảng (gồm nhịn ăn), snapshot expenditure.
    /// <paramref name="from"/> null = ngày có dữ liệu đầu tiên (log đầu tiên hoặc ngày đầu của chuỗi expenditure). Target của MỖI NGÀY lấy từ tuần program hiệu lực tại ngày đó.
    /// </summary>
    public static async Task<(DateOnly From, List<EnergyBalanceDay> Days)> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly? from, DateOnly to, AlgorithmOptions opt, CancellationToken ct, DateOnly? today = null)
    {
        if (from is { } f && to.DayNumber - f.DayNumber + 1 > MaxRangeDays) throw new BadRequestException(ValidationMessages.RangeTooLong(MaxRangeDays));

        // Target của mỗi ngày theo tuần program hiệu lực NGÀY ĐÓ (kể cả program đã kết thúc); chỉ mốc bắt đầu expenditure lấy từ program đang active
        var programs = await ProgramHistory.LoadAsync(uow, userId, from, to, ct);
        var program = programs.FirstOrDefault(p => p.IsActive);

        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, from ?? DateOnly.MinValue, to, ct, includeFasting: true);
        var raw = await ExpenditureSeriesSupport.BuildAsync(uow, userId, to, program?.ExpenditureStartDate, opt, ct);

        var first = from ?? new[] { logs.Count > 0 ? logs.Keys.Min() : to, raw.Count > 0 ? raw[0].Date : to }.Min();
        var expenditure = ExpenditureSeries.ToDays(raw, opt.KcalPerKg, opt.FluxTrendErrorKg).ToDictionary(d => d.Date, d => d.ExpenditureKcal);
        return (first, EnergyBalanceCalculator.Days(first, to, logs, expenditure, TargetFn(programs), d => ProgramHistory.ProvenanceOn(programs, d, today)));
    }

    /// <summary>
    /// Target kcal của một ngày: tuần hiệu lực tại ngày đó trong các program của user, kể cả đã kết thúc (<see cref="ProgramHistory.EffectiveOn"/>) + dạng ngày trong tuần;
    /// null khi chưa có program nào phủ ngày đó. Cache theo (tuần, thứ).
    /// </summary>
    public static Func<DateOnly, decimal?> TargetFn(IReadOnlyList<MacroProgram> programs) => TargetFn(programs, w => w.CaloriesTarget);

    /// <summary>Như <see cref="TargetFn(IReadOnlyList{MacroProgram})"/> nhưng chọn chỉ tiêu bằng <paramref name="pick"/> (protein/carbs/fat của trang chi tiết nutrient).</summary>
    public static Func<DateOnly, decimal?> TargetFn(IReadOnlyList<MacroProgram> programs, Func<ProgramWeekModel, decimal> pick)
    {
        var cache = new Dictionary<(Guid, DayOfWeek), decimal>();
        return d =>
        {
            if (ProgramHistory.EffectiveOn(programs, d) is not { } effective) return null;
            var key = (effective.Week.Id, d.DayOfWeek);
            if (!cache.TryGetValue(key, out var kcal)) cache[key] = kcal = pick(ProgramTargetResolver.ForDay(effective.Week, effective.Program, d.DayOfWeek));
            return kcal;
        };
    }
}
