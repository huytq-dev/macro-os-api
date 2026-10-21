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

    /// <summary>Floor/Target/Ceiling theo ngày: Calories/P/F/C = target của program CỦA NGÀY ĐÓ (không floor/ceiling); net carbs không có mức; chất khác = mức hiệu lực (user ghi đè hoặc mặc định) tại <paramref name="to"/>.</summary>
    public static async Task<(Func<DateOnly, (decimal? Floor, decimal? Target, decimal? Ceiling)> Goal, string Kind, DateOnly? FirstTargetDate)> GoalFnAsync(IUnitOfWork uow, Guid userId, string code, DateOnly to, CancellationToken ct)
    {
        if (NutrientCatalog.MacroCodes.Contains(code))
        {
            var program = await uow.Repository<MacroProgram>().Query()
                .Include(p => p.Weeks).Include(p => p.DayTargets)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.IsActive, ct);
            var target = EnergyBalanceSupport.TargetFn(program, code switch
            {
                "energy_kcal" => w => w.CaloriesTarget,
                "protein_g" => w => w.ProteinTarget,
                "carbs_g" => w => w.CarbsTarget,
                _ => w => w.FatTarget,
            });
            // Calories/P/F/C có target từ tuần program đầu tiên: ngày đầu của chuỗi `all` lùi tới đó để đường target chạy qua mọi ngày có program
            return (d => (null, target(d), null), NutrientGoals.KindOf(null, target(to), null, NutrientCatalog.Basis.Program), program?.Weeks.Count > 0 ? program.Weeks.Min(w => w.EffectiveFrom) : null);
        }
        if (code == NutrientCatalog.NetCarbsCode) return (_ => (null, null, null), NutrientGoals.None, null);

        var item = (await NutrientCatalog.BuildAsync(uow, userId, to, withAmounts: false, ct, includeExtended: true)).FirstOrDefault(i => i.Code == code);
        var goal = (item?.Floor, item?.Target, item?.Ceiling);
        return (_ => goal, item?.GoalKind ?? NutrientGoals.None, null); // vi chất: mức hiện tại áp mọi ngày → không có ngày bắt đầu riêng (không tạo vô hạn ngày)
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
        Func<DateOnly, (decimal? Floor, decimal? Target, decimal? Ceiling)> goal, TimeProvider tp, CancellationToken ct, DateOnly? firstTargetDate = null)
    {
        if (from is { } f && to.DayNumber - f.DayNumber + 1 > MaxRangeDays) throw new BadRequestException($"Range must not exceed {MaxRangeDays} days.");

        var logs = await FoodLogSupport.LoadDaysAsync(uow, userId, from ?? DateOnly.MinValue, to, ct, includeFasting: true);
        var first = from ?? FirstDay(logs.Count > 0 ? logs.Keys.Min() : null, firstTargetDate, to);
        var amount = await AmountFnAsync(uow, userId, code, first, to, logs, tp, ct);
        return (first, NutrientDetailCalculator.Days(first, to, logs, amount, NutrientDetailCalculator.ZeroOnFastingCodes.Contains(code), goal, NutrientDetailCalculator.Decimals(code)));
    }
}

/// <summary>
/// Trang chi tiết nutrient — biểu đồ lịch sử: mỗi ngày LIÊN TỤC từ From đến To (mặc định và tối đa hôm nay theo múi giờ user) với lượng đã ăn, target CỦA NGÀY ĐÓ và dải Range (floor→ceiling),
/// thống kê khoảng (trung bình mỗi ngày đã log) và bucket Day/Week/Month khi có Granularity. From mặc định 90 ngày trước To; All = true và không From: Calories/P/F/C từ ngày sớm hơn giữa log đầu tiên và tuần program đầu tiên, vi chất từ ngày log đầu tiên. Mã không biết → 404.
/// </summary>
public sealed record GetNutrientSeriesQuery(string Code, DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<NutrientSeriesResponse>;

public sealed class GetNutrientSeriesQueryValidator : AbstractValidator<GetNutrientSeriesQuery>
{
    public GetNutrientSeriesQueryValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
        RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage("From must not be after To.");
    }
}

public sealed class GetNutrientSeriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetNutrientSeriesQuery, NutrientSeriesResponse>
{
    public async Task<NutrientSeriesResponse> Handle(GetNutrientSeriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var nutrient = await NutrientDetailSupport.ResolveAsync(_uow, request.Code, ct);
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To is { } t && t < locale.Today ? t : locale.Today;
        DateOnly? from = request.From ?? (request.All == true ? null : to.AddDays(-90));

        var (goal, goalKind, firstTargetDate) = await NutrientDetailSupport.GoalFnAsync(_uow, userId, nutrient.Code, to, ct);
        var (first, days) = await NutrientDetailSupport.LoadAsync(_uow, userId, nutrient.Code, from, to, goal, _timeProvider, ct, firstTargetDate);
        var decimals = NutrientDetailCalculator.Decimals(nutrient.Code);
        var buckets = request.Granularity is { } g ? NutrientDetailCalculator.Buckets(days, g, locale.WeekStart, decimals) : [];
        return new NutrientSeriesResponse(nutrient.Code, nutrient.Name, nutrient.Unit, nutrient.Precision, goalKind, first, to, days, NutrientDetailCalculator.Summary(days, first, to, decimals), buckets);
    }
}

/// <summary>
/// Trang chi tiết nutrient cho một ngày (mặc định hôm nay, không ở tương lai theo múi giờ user): lượng + mức hiệu lực + nguồn mục tiêu (Strategy/Custom/Default/None),
/// trung bình Past Week (7 ngày) / Past Month (30 ngày) kết thúc ở ngày đó (mỗi ngày đã log, không tính ngày chưa đủ), mọi Contributors giảm dần và Nutrient Timing theo giờ (24 khung). Mã không biết → 404.
/// </summary>
public sealed record GetNutrientDayDetailQuery(string Code, DateOnly? Date) : IRequest<NutrientDayDetailResponse>;

public sealed class GetNutrientDayDetailQueryValidator : AbstractValidator<GetNutrientDayDetailQuery>
{
    public GetNutrientDayDetailQueryValidator() => RuleFor(x => x.Code).NotEmpty().MaximumLength(64);
}

public sealed class GetNutrientDayDetailQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetNutrientDayDetailQuery, NutrientDayDetailResponse>
{
    public async Task<NutrientDayDetailResponse> Handle(GetNutrientDayDetailQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var date = request.Date ?? locale.Today;
        locale.EnsureNotFuture(date, "Date");

        // Cùng nguồn và quy tắc với GET /nutrients?date= (ngày chưa log: macro/fiber/sodium 0, vi chất null), kèm mức hiệu lực + nguồn mục tiêu
        var item = (await NutrientCatalog.BuildAsync(_uow, userId, date, withAmounts: true, ct, includeExtended: true)).FirstOrDefault(i => i.Code == request.Code)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("Nutrient", request.Code));
        var decimals = NutrientDetailCalculator.Decimals(item.Code);

        // Past Week / Past Month: 30 ngày kết thúc ở date, cùng đường tính với biểu đồ (không cần target)
        var (_, window) = await NutrientDetailSupport.LoadAsync(_uow, userId, item.Code, date.AddDays(-29), date, _ => (null, null, null), _timeProvider, ct);
        var week = NutrientDetailCalculator.WindowAverage(window, date, 7, decimals);
        var month = NutrientDetailCalculator.WindowAverage(window, date, 30, decimals);

        var (contributors, timing) = await EntriesAsync(userId, item.Code, date, decimals, ct);
        return new NutrientDayDetailResponse(item.Code, item.Name, item.Unit, item.DisplayPrecision, date,
            item.Amount is { } a ? NutritionRounding.Round(a, decimals) : null, item.Floor, item.Target, item.Ceiling, item.GoalKind,
            NutrientDetailCalculator.TargetSource(item.TargetBasis), item.TargetBasis, week.Average, week.LoggedDays, month.Average, month.LoggedDays, contributors, timing);
    }

    /// <summary>Hai truy vấn: entry của ngày (tên, bữa, giờ, cột macro/fiber/sodium) và, với vi chất, các dòng FoodLogEntryNutrient của chất đó.</summary>
    private async Task<(List<NutrientContributorModel> Contributors, List<NutrientTimingModel> Timing)> EntriesAsync(Guid userId, string code, DateOnly date, int decimals, CancellationToken ct)
    {
        var entries = await _uow.Repository<FoodLogEntry>().Query()
            .Where(e => e.FoodLog.UserId == userId && e.FoodLog.LogDate == date)
            .Select(e => new
            {
                e.Id, Name = e.FoodItem != null ? e.FoodItem.Name : e.Recipe != null ? e.Recipe.Name : "",
                e.MealType, e.EatenAt, e.Calories, e.Protein, e.Carbs, e.Fat, e.Fiber, e.Sodium,
            })
            .ToListAsync(ct);

        Dictionary<Guid, decimal> micro = [];
        if (!NutrientDetailCalculator.ZeroOnFastingCodes.Contains(code))
            micro = (await _uow.Repository<FoodLogEntryNutrient>().Query()
                .Where(n => n.FoodLogEntry.FoodLog.UserId == userId && n.FoodLogEntry.FoodLog.LogDate == date && !n.FoodLogEntry.IsDeleted && n.Nutrient.Code == code && n.Amount != null)
                .GroupBy(n => n.FoodLogEntryId)
                .Select(g => new { Id = g.Key, Sum = g.Sum(n => n.Amount!.Value) })
                .ToListAsync(ct)).ToDictionary(r => r.Id, r => r.Sum);

        var amounts = entries.Select(e => (
            e.Id, e.Name, Time: e.EatenAt ?? MealTimes.Default(e.MealType),
            Amount: code switch
            {
                "energy_kcal" => e.Calories,
                "protein_g" => e.Protein,
                "carbs_g" => e.Carbs,
                "fat_g" => e.Fat,
                "fiber_g" => e.Fiber,
                "sodium_mg" => e.Sodium,
                NutrientCatalog.NetCarbsCode => e.Fiber is { } f ? NutritionCalculator.NetCarbs(e.Carbs, f) : null,
                _ => micro.TryGetValue(e.Id, out var v) ? v : (decimal?)null,
            })).ToList();

        return (NutrientDetailCalculator.Contributors(amounts.Select(a => (a.Id, a.Name, a.Amount)), decimals),
            NutrientDetailCalculator.Timing(amounts.Select(a => (a.Time, a.Amount)), decimals));
    }
}
