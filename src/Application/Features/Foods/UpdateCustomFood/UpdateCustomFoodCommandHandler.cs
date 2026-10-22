namespace Application;

public sealed class UpdateCustomFoodCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<UpdateCustomFoodCommand, Unit>
{
    public async Task<Unit> Handle(UpdateCustomFoodCommand request, CancellationToken ct)
    {
        var food = await _uow.Repository<FoodItem>().Query(tracking: true)
            .Include(f => f.Servings)
            .FirstOrDefaultAsync(f => f.Id == request.Id, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", request.Id));

        if (food.Source != FoodSource.UserCreated || food.CreatedByUserId != _userContext.UserId)
            throw new ForbiddenException(ExceptionMessages.OnlyEditOwnFood);

        food.Name = request.Name.Trim();
        food.Brand = request.Brand?.Trim();
        food.Barcode = request.Barcode?.Trim();
        food.Category = request.Category;
        if (request.IconKey is not null) food.IconKey = request.IconKey; // không gửi = giữ icon hiện tại
        FoodNutritionApplier.Apply(food, request.Nutrition);
        if (request.Servings is { Count: > 0 })
            FoodNutritionApplier.ReplaceServings(food, request.Servings);

        await FoodNutrientStore.SaveAsync(_uow, food.Id, FoodNutrientStore.Amounts(request.Nutrition, request.Nutrients),
            replaceAll: request.Nutrients is not null, isNew: false, await NutrientSnapshot.CodeIdsAsync(_uow, ct), ct);

        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
