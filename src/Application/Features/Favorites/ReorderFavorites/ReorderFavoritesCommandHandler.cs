namespace Application;

public sealed class ReorderFavoritesCommandHandler(IUnitOfWork _uow, IUserContext _userContext)
    : IRequestHandler<ReorderFavoritesCommand, Unit>
{
    public async Task<Unit> Handle(ReorderFavoritesCommand request, CancellationToken ct)
    {
        var userId = _userContext.UserId;
        var favorites = await _uow.Repository<FoodFavorite>().Query(tracking: true)
            .Where(f => f.UserId == userId)
            .ToListAsync(ct);

        var position = request.Ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
        // Món không có trong danh sách gửi lên giữ nguyên thứ tự tương đối, xếp sau các món đã sắp xếp.
        var ordered = favorites
            .OrderBy(f => position.TryGetValue(f.Id, out var index) ? index : int.MaxValue)
            .ThenBy(f => f.SortOrder);

        var order = 0;
        foreach (var favorite in ordered) favorite.SortOrder = order++;

        await _uow.EnsureSaveAsync(ct);
        return Unit.Value;
    }
}
