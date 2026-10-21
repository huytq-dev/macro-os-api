namespace Application;

public sealed class GetEntryNutrientsQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetEntryNutrientsQuery, EntryNutrientsResponse>
{
    public async Task<EntryNutrientsResponse> Handle(GetEntryNutrientsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var e = await _uow.Repository<FoodLogEntry>().Query()
            .Where(x => x.Id == request.Id && x.FoodLog.UserId == userId)
            .Select(x => new
            {
                x.Quantity, x.Calories, x.Protein, x.Carbs, x.Fat, x.Fiber, x.Sodium,
                Date = x.FoodLog.LogDate,
                Name = x.FoodItem != null ? x.FoodItem.Name : x.Recipe != null ? x.Recipe.Name : "",
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodLogEntry", request.Id));

        var rows = await _uow.Repository<FoodLogEntryNutrient>().Query()
            .Where(n => n.FoodLogEntryId == request.Id && n.Amount != null)
            .Select(n => new { n.Nutrient.Code, Amount = n.Amount!.Value })
            .ToListAsync(ct);

        var amounts = rows.ToDictionary(r => r.Code, r => r.Amount);
        amounts["energy_kcal"] = e.Calories;
        amounts["protein_g"] = e.Protein;
        amounts["carbs_g"] = e.Carbs;
        amounts["fat_g"] = e.Fat;
        if (e.Fiber is { } fiber) amounts["fiber_g"] = fiber;
        if (e.Sodium is { } sodium) amounts["sodium_mg"] = sodium;

        var items = await NutrientBreakdown.BuildAsync(_uow, userId, e.Date, amounts, ct);
        return new EntryNutrientsResponse(request.Id, e.Name, e.Date, e.Quantity, items);
    }
}
