namespace Application;

/// <summary>
/// Multi-Select / View: dinh dưỡng của các entry đã chọn (đọc từ snapshot lúc log: macro/fiber/sodium từ cột entry, còn lại từ FoodLogEntryNutrient).
/// Cùng shape với plate/nutrients: <c>Plate</c> = tổng các entry đã chọn, <c>Day</c> = tổng đã log của <c>Date</c> (đã gồm các entry đó, nên không cộng thêm).
/// <c>Date</c> thiếu → ngày có nhiều entry được chọn nhất (hoà thì ngày sớm hơn); mục tiêu % tính theo ngày này. Id không phải của mình bị bỏ qua; không có id nào là của mình → 404.
/// </summary>
public sealed record GetEntriesNutrientsQuery(List<Guid> Ids, DateOnly? Date = null) : IRequest<PlateNutrientsResponse>;

public sealed class GetEntriesNutrientsQueryValidator : AbstractValidator<GetEntriesNutrientsQuery>
{
    public GetEntriesNutrientsQueryValidator() => BulkEntrySupport.Ids(this, x => x.Ids);
}

public sealed class GetEntriesNutrientsQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetEntriesNutrientsQuery, PlateNutrientsResponse>
{
    public async Task<PlateNutrientsResponse> Handle(GetEntriesNutrientsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var ids = request.Ids.Distinct().ToList();
        var entries = await _uow.Repository<FoodLogEntry>().Query()
            .Where(e => ids.Contains(e.Id) && e.FoodLog.UserId == userId)
            .Select(e => new { e.Id, e.Calories, e.Protein, e.Carbs, e.Fat, e.Fiber, e.Sodium, Date = e.FoodLog.LogDate })
            .ToListAsync(ct);
        if (entries.Count == 0) throw new NotFoundException(ExceptionMessages.NotFound("FoodLogEntry", ids[0]));

        var ownedIds = entries.Select(e => e.Id).ToList();
        var rows = (await _uow.Repository<FoodLogEntryNutrient>().Query()
            .Where(n => ownedIds.Contains(n.FoodLogEntryId) && n.Amount != null)
            .Select(n => new { n.FoodLogEntryId, n.Nutrient.Code, Amount = n.Amount!.Value })
            .ToListAsync(ct)).ToLookup(n => n.FoodLogEntryId);

        var parts = new List<IReadOnlyDictionary<string, decimal>>(entries.Count);
        foreach (var e in entries)
        {
            var amounts = rows[e.Id].ToDictionary(r => r.Code, r => r.Amount);
            amounts["energy_kcal"] = e.Calories;
            amounts["protein_g"] = e.Protein;
            amounts["carbs_g"] = e.Carbs;
            amounts["fat_g"] = e.Fat;
            if (e.Fiber is { } fiber) amounts["fiber_g"] = fiber;
            if (e.Sodium is { } sodium) amounts["sodium_mg"] = sodium;
            parts.Add(amounts);
        }
        var selection = NutritionCalculator.SumAmounts(parts);

        var date = request.Date ?? entries.GroupBy(e => e.Date).OrderByDescending(g => g.Count()).ThenBy(g => g.Key).First().Key;
        var logged = await NutrientCatalog.SumsAsync(_uow, userId, date, date, ct);
        var day = logged.Where(kv => kv.Value is not null).ToDictionary(kv => kv.Key, kv => kv.Value!.Value);

        return new PlateNutrientsResponse(
            date,
            await NutrientBreakdown.BuildAsync(_uow, userId, date, selection, ct),
            await NutrientBreakdown.BuildAsync(_uow, userId, date, day, ct));
    }
}
