namespace Application;

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
        // Calories/P/F/C: target lấy từ program của ngày `date`: kèm provenance (VERIFIED | LEGACY_UNVERIFIED); chất khác không phụ thuộc lịch sử program
        var provenance = NutrientCatalog.MacroCodes.Contains(item.Code) && item.Target is not null
            ? (await FoodLogSupport.TargetForDateAsync(_uow, userId, date, ct, locale.Today))?.TargetProvenance : null;
        return new NutrientDayDetailResponse(item.Code, item.Name, item.Unit, item.DisplayPrecision, date,
            item.Amount is { } a ? NutritionRounding.Round(a, decimals) : null, item.Floor, item.Target, item.Ceiling, item.GoalKind,
            NutrientDetailCalculator.TargetSource(item.TargetBasis), item.TargetBasis, week.Average, week.LoggedDays, month.Average, month.LoggedDays, contributors, timing, provenance);
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
