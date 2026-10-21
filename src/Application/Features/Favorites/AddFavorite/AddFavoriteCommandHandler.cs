namespace Application;

public sealed class AddFavoriteCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<AddFavoriteCommand, Guid>
{
    public async Task<Guid> Handle(AddFavoriteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;

        var food = await _uow.Repository<FoodItem>().FindAsync(f => f.Id == request.FoodItemId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", request.FoodItemId));
        // Custom food của người khác coi như không tồn tại
        if (food.Source == FoodSource.UserCreated && food.CreatedByUserId != userId)
            throw new NotFoundException(ExceptionMessages.NotFound("FoodItem", request.FoodItemId));

        if (request.FoodServingId is { } servingId
            && !await _uow.Repository<FoodServing>().Query().AnyAsync(s => s.Id == servingId && s.FoodItemId == food.Id, ct))
            throw new NotFoundException(ExceptionMessages.NotFound("FoodServing", servingId));

        var repository = _uow.Repository<FoodFavorite>();
        var existing = await repository.Query()
            .Where(f => f.UserId == userId && f.FoodItemId == food.Id
                && f.FoodServingId == request.FoodServingId && f.Quantity == request.Quantity)
            .Select(f => (Guid?)f.Id)
            .FirstOrDefaultAsync(ct);
        if (existing is { } id) return id;

        var nextOrder = (await repository.Query().Where(f => f.UserId == userId).MaxAsync(f => (int?)f.SortOrder, ct) ?? -1) + 1;
        var favorite = new FoodFavorite
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FoodItemId = food.Id,
            FoodServingId = request.FoodServingId,
            Quantity = request.Quantity,
            SortOrder = nextOrder,
        };
        await repository.AddAsync(favorite, ct);
        await _uow.EnsureSaveAsync(ct);
        return favorite.Id;
    }
}
