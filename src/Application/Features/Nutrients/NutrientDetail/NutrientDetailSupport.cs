namespace Application;

public static class NutrientDetailSupport
{
    public const int MaxRangeDays = 3660;

    public sealed record NutrientRef(string Code, string Name, string Unit, int Precision);

    /// <summary>Nutrient theo mã (gồm chất chi tiết và <c>carbs_net_g</c>); chất ẩn hoặc không có → 404.</summary>
    public static async Task<NutrientRef> ResolveAsync(IUnitOfWork uow, string code, CancellationToken ct)
    {
        if (code == NutrientCatalog.NetCarbsCode) return new NutrientRef(code, "Net (Non-fiber)", "g", 1);
        var def = NutrientCatalog.HiddenCodes.Contains(code) ? null
            : await uow.Repository<NutrientDefinition>().Query().Where(d => d.Code == code)
                .Select(d => new { d.Name, d.Unit, d.DisplayPrecision }).FirstOrDefaultAsync(ct);
        return def is null ? throw new NotFoundException(ExceptionMessages.NotFound("Nutrient", code)) : new NutrientRef(code, def.Name, def.Unit, def.DisplayPrecision);
    }

    /// <summary>
    /// Lượng của <paramref name="code"/> trong từng ngày ĐÃ LOG: Calories/P/C/F từ tổng FoodLog (cùng nguồn với Energy Balance), chất khác từ NutritionDayAggregate (một truy vấn cho cả khoảng;
    /// ngày không có dữ liệu chất đó trả null). Net carbs = carbs − fiber khi ngày có dữ liệu fiber.
    /// </summary>
    public static async Task<Func<DateOnly, decimal?>> AmountFnAsync(IUnitOfWork uow, Guid userId, string code, DateOnly from, DateOnly to,
        IReadOnlyDictionary<DateOnly, FoodLogSupport.LoggedDay> logs, TimeProvider tp, CancellationToken ct)
    {
        switch (code)
        {
            case "energy_kcal": return d => logs[d].TotalCalories;
            case "protein_g": return d => logs[d].TotalProtein;
            case "carbs_g": return d => logs[d].TotalCarbs;
            case "fat_g": return d => logs[d].TotalFat;
        }

        var perDay = await NutritionAggregates.PerDayAsync(uow, userId, from, to, logs.Values.Where(l => !l.IsFasting).Select(l => l.Date).ToList(), tp, ct);
        if (code == NutrientCatalog.NetCarbsCode)
            return d => perDay.TryGetValue(d, out var m) && m.TryGetValue("carbs_g", out var carbs) && m.TryGetValue("fiber_g", out var fiber) ? NutritionCalculator.NetCarbs(carbs, fiber) : null;
        return d => perDay.TryGetValue(d, out var m) && m.TryGetValue(code, out var v) ? v : null;
    }

    /// <summary>Floor/Target/Ceiling theo ngày: Calories/P/F/C = target của program CỦA NGÀY ĐÓ (không floor/ceiling); net carbs không có mức; chất khác = mức hiệu lực CỦA NGÀY ĐÓ (user ghi đè theo lịch sử, thiếu thì mặc định seed/DRI tại <paramref name="to"/>).</summary>
    /// <returns>Provenance: provenance target program từng ngày (VERIFIED | LEGACY_UNVERIFIED) cho Calories/P/F/C; null cho chất khác (mức không phụ thuộc lịch sử program).</returns>
    public static async Task<(Func<DateOnly, (decimal? Floor, decimal? Target, decimal? Ceiling)> Goal, string Kind, DateOnly? FirstTargetDate, Func<DateOnly, string?>? Provenance)> GoalFnAsync(IUnitOfWork uow, Guid userId, string code, DateOnly to, CancellationToken ct, DateOnly? today = null)
    {
        if (NutrientCatalog.MacroCodes.Contains(code))
        {
            var programs = await ProgramHistory.LoadAsync(uow, userId, null, to, ct);
            var target = EnergyBalanceSupport.TargetFn(programs, code switch
            {
                "energy_kcal" => w => w.CaloriesTarget,
                "protein_g" => w => w.ProteinTarget,
                "carbs_g" => w => w.CarbsTarget,
                _ => w => w.FatTarget,
            });
            // Calories/P/F/C có target từ tuần program đầu tiên: ngày đầu của chuỗi `all` lùi tới đó để đường target chạy qua mọi ngày có program
            return (d => (null, target(d), null), NutrientGoals.KindOf(null, target(to), null, NutrientCatalog.Basis.Program), programs.SelectMany(p => p.Weeks).Select(w => (DateOnly?)w.EffectiveFrom).Min(),
                d => ProgramHistory.ProvenanceOn(programs, d, today));
        }
        if (code == NutrientCatalog.NetCarbsCode) return (_ => (null, null, null), NutrientGoals.None, null, null);

        // Vi chất: mức mặc định (seed + DRI) + ghi đè tuỳ chỉnh THEO NGÀY từ lịch sử (ngày cũ giữ mức cũ khi user đổi goal; reset về mặc định cũng là một lần đổi)
        var def = await uow.Repository<NutrientDefinition>().Query().FirstOrDefaultAsync(d => d.Code == code, ct);
        if (def is null) return (_ => (null, null, null), NutrientGoals.None, null, null);
        var baseline = await NutrientCatalog.DefaultGoalAsync(uow, userId, def, to, ct);
        var changes = (await NutrientGoalHistory.LoadAsync(uow, userId, to, ct)).GetValueOrDefault(def.Id) ?? [];
        NutrientGoal On(DateOnly d) => NutrientGoalHistory.On(changes, d) is { } u ? NutrientGoals.Make(u.Floor, u.Target, u.Ceiling, NutrientCatalog.Basis.User) : baseline;
        return (d => { var g = On(d); return (g.Floor, g.Target, g.Ceiling); }, On(to).Kind, null, null); // không có ngày bắt đầu riêng (không tạo vô hạn ngày)
    }

    /// <summary>
    /// Ngày đầu của chuỗi khi không gửi from (all=true): sớm hơn trong ngày log đầu tiên và ngày đầu tiên có target (tuần program đầu tiên, chỉ Calories/P/F/C) để đường target chạy qua mọi ngày có program;
    /// chưa có gì thì <paramref name="to"/>; không vượt <see cref="MaxRangeDays"/> ngày (lùi từ <paramref name="to"/>, không 400). Ngày có target sau <paramref name="to"/> bị bỏ qua.
    /// </summary>
    public static DateOnly FirstDay(DateOnly? firstLog, DateOnly? firstTarget, DateOnly to)
    {
        var first = new[] { firstLog, firstTarget is { } t && t <= to ? t : (DateOnly?)null }.Where(d => d.HasValue).Select(d => d!.Value).DefaultIfEmpty(to).Min();
        var earliest = to.AddDays(-(MaxRangeDays - 1));
        return first < earliest ? earliest : first;
    }

    /// <summary>Các ngày liên tục [from, to] của một nutrient. <paramref name="from"/> null = ngày log đầu tiên (hoặc <paramref name="to"/> nếu chưa log gì). Số truy vấn cố định, không phụ thuộc độ dài khoảng.</summary>
    public static async Task<(DateOnly From, List<NutrientSeriesDay> Days)> LoadAsync(IUnitOfWork uow, Guid userId, string code, DateOnly? from, DateOnly to,
        Func<DateOnly, (decimal? Floor, decimal? Target, decimal? Ceiling)> goal, TimeProvider tp, CancellationToken ct, DateOnly? firstTargetDate = null, Func<DateOnly, string?>? provenance = null)
    {
        if (from is { } f && to.DayNumber - f.DayNumber + 1 > MaxRangeDays) throw new BadRequestException(ValidationMessages.RangeTooLong(MaxRangeDays));

        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, from ?? DateOnly.MinValue, to, ct, includeFasting: true);
        var first = from ?? FirstDay(logs.Count > 0 ? logs.Keys.Min() : null, firstTargetDate, to);
        var amount = await AmountFnAsync(uow, userId, code, first, to, logs, tp, ct);
        return (first, NutrientDetailCalculator.Days(first, to, logs, amount, NutrientDetailCalculator.ZeroOnFastingCodes.Contains(code), goal, NutrientDetailCalculator.Decimals(code), provenance));
    }
}
