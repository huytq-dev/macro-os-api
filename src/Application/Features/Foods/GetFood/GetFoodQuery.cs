namespace Application;

public sealed record GetFoodQuery(Guid Id) : IRequest<FoodItemResponse>;

public sealed class GetFoodQueryHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<GetFoodQuery, FoodItemResponse>
{
    public async Task<FoodItemResponse> Handle(GetFoodQuery request, CancellationToken ct)
    {
        var food = await _uow.Repository<FoodItem>().FindAsync(f => f.Id == request.Id, ct, f => f.Servings)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", request.Id));

        // Custom food của người khác coi như không tồn tại
        if (food.Source == FoodSource.UserCreated && food.CreatedByUserId != _userContext.UserId)
            throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", request.Id));

        return await FoodNutrientStore.ToResponseAsync(_uow, food, food.CreatedByUserId == _userContext.UserId, _userContext.UserId, ct);
    }
}
