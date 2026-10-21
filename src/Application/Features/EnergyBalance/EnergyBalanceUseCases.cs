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
    public static Func<DateOnly, decimal?> TargetFn(MacroProgram? program)
    {
        if (program is null) return _ => null;
        var weeks = program.Weeks.OrderByDescending(w => w.EffectiveFrom).ToList();
        var cache = new Dictionary<(Guid, DayOfWeek), decimal>();
        return d =>
        {
            var week = weeks.FirstOrDefault(w => w.EffectiveFrom <= d);
            if (week is null) return null;
            var key = (week.Id, d.DayOfWeek);
            if (!cache.TryGetValue(key, out var kcal)) cache[key] = kcal = ProgramTargetResolver.ForDay(week, program, d.DayOfWeek).CaloriesTarget;
            return kcal;
        };
    }
}

/// <summary>
/// Trang Energy Balance (hai tab Expenditure / Calorie Targets): mỗi ngày LIÊN TỤC từ From đến To (mặc định và tối đa hôm nay theo múi giờ user) với kcal ăn, expenditure và target của ngày đó,
/// chênh lệch, thống kê khoảng (trung bình mỗi ngày đã log) và bucket Day/Week/Month khi có Granularity. From mặc định 90 ngày trước To; All = true và không From: từ ngày có dữ liệu đầu tiên.
/// </summary>
public sealed record GetEnergyBalanceSeriesQuery(DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<EnergyBalanceSeriesResponse>;

public sealed class GetEnergyBalanceSeriesQueryValidator : AbstractValidator<GetEnergyBalanceSeriesQuery>
{
    public GetEnergyBalanceSeriesQueryValidator()
        => RuleFor(x => x).Must(x => x.From is null || x.To is null || x.From <= x.To).WithName("From").WithMessage("From must not be after To.");
}

public sealed class GetEnergyBalanceSeriesQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetEnergyBalanceSeriesQuery, EnergyBalanceSeriesResponse>
{
    public async Task<EnergyBalanceSeriesResponse> Handle(GetEnergyBalanceSeriesQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var locale = await UserLocale.GetAsync(_uow, userId, _timeProvider, ct);
        var to = request.To is { } t && t < locale.Today ? t : locale.Today;
        DateOnly? from = request.From ?? (request.All == true ? null : to.AddDays(-90));

        var (first, days) = await EnergyBalanceSupport.LoadAsync(_uow, userId, from, to, _options.Value, ct);
        var summary = EnergyBalanceCalculator.Summary(days, first, to);
        var buckets = request.Granularity is { } g ? EnergyBalanceCalculator.Buckets(days, g, locale.WeekStart) : [];
        return new EnergyBalanceSeriesResponse(first, to, days, summary, buckets);
    }
}

/// <summary>
/// Insights &amp; Data của Energy Balance: Relative to Expenditure và Relative to Targets cho 3/7/14/30/90 ngày kết thúc hôm nay (theo múi giờ user): trung bình chênh lệch mỗi ngày đã log
/// hợp lệ trong cửa sổ, nhãn Deficit/Surplus/Balanced và BelowTarget/AboveTarget/OnTarget; Waiting khi cửa sổ chưa có ngày nào dùng được.
/// </summary>
public sealed record GetEnergyBalanceInsightsQuery : IRequest<EnergyBalanceInsightsResponse>;

public sealed class GetEnergyBalanceInsightsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider, IOptions<AlgorithmOptions> _options)
    : IRequestHandler<GetEnergyBalanceInsightsQuery, EnergyBalanceInsightsResponse>
{
    public async Task<EnergyBalanceInsightsResponse> Handle(GetEnergyBalanceInsightsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;
        var (_, days) = await EnergyBalanceSupport.LoadAsync(_uow, userId, today.AddDays(-(EnergyBalanceCalculator.Windows.Max() - 1)), today, _options.Value, ct);
        return new EnergyBalanceInsightsResponse(today, EnergyBalanceCalculator.Relative(days, today, false), EnergyBalanceCalculator.Relative(days, today, true));
    }
}
