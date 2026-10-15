namespace Application;

public sealed class CreateCustomFoodCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<CreateCustomFoodCommand, Guid>
{
    public async Task<Guid> Handle(CreateCustomFoodCommand request, CancellationToken ct)
    {
        var food = new FoodItem
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Brand = request.Brand?.Trim(),
            Barcode = request.Barcode?.Trim(),
            Category = request.Category,
            Source = FoodSource.UserCreated,
            CreatedByUserId = _userContext.UserId,
            IsVerified = false,
        };
        FoodNutritionApplier.Apply(food, request.Nutrition);
        FoodNutritionApplier.ReplaceServings(food,
            request.Servings is { Count: > 0 } ? request.Servings : [new FoodServingInput("100 g", 100, true)]);

        await _uow.Repository<FoodItem>().AddAsync(food, ct);
        await _uow.EnsureSaveAsync(ct);
        return food.Id;
    }
}
