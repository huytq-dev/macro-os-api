namespace Application;

public sealed record RemoveFavoriteCommand(Guid Id) : IRequest<Unit>;

public sealed class RemoveFavoriteCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<RemoveFavoriteCommand, Unit>
{
    public async Task<Unit> Handle(RemoveFavoriteCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        // Của người khác coi như không tồn tại
        var favorite = await _uow.Repository<FoodFavorite>().FindTrackedAsync(f => f.Id == request.Id && f.UserId == userId, ct)
            ?? throw new NotFoundException(ExceptionMessages.NotFound("FoodFavorite", request.Id));

        await _uow.Repository<FoodFavorite>().RemoveAsync(favorite, ct);
        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
