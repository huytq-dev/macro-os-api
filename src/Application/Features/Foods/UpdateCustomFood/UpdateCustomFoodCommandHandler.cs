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
            throw new ForbiddenException("Chỉ sửa được thực phẩm do bạn tạo.");

        food.Name = request.Name.Trim();
        food.Brand = request.Brand?.Trim();
        food.Barcode = request.Barcode?.Trim();
        food.Category = request.Category;
        FoodNutritionApplier.Apply(food, request.Nutrition);
        if (request.Servings is { Count: > 0 })
            FoodNutritionApplier.ReplaceServings(food, request.Servings);

        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
