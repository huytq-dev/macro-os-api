namespace Application;

public sealed class GetFoodNutrientsQueryHandler(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider)
    : IRequestHandler<GetFoodNutrientsQuery, FoodNutrientsResponse>
{
    public async Task<FoodNutrientsResponse> Handle(GetFoodNutrientsQuery request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var food = await FoodLogSupport.GetLoggableFoodAsync(_uow, userId, request.Id, ct);

        var serving = request.ServingId is { } sid
            ? food.Servings.FirstOrDefault(s => s.Id == sid) ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodServing", sid))
            : food.Servings.OrderByDescending(s => s.IsDefault).FirstOrDefault();
        var grams = FoodLogSupport.Grams(request.Quantity, serving);
        var date = request.Date ?? FoodLogSupport.Today(_timeProvider);

        var profile = await NutrientSnapshot.LoadProfileAsync(_uow, food, ct);
        var amounts = NutritionCalculator.ScaleProfile(profile, NutritionCalculator.GramFactor(grams)).ToDictionary(p => p.Key, p => p.Value.Amount);

        var items = await NutrientBreakdown.BuildAsync(_uow, userId, date, amounts, ct);
        return new FoodNutrientsResponse(food.Id, food.Name, serving?.Id, serving?.ServingName ?? "100 g", request.Quantity, grams, date, items);
    }
}
