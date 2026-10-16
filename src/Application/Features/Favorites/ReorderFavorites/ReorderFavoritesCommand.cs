namespace Application;

/// <summary>Sắp xếp lại yêu thích theo thứ tự `Ids` (Library → Favorites → Reorder). Id không thuộc người dùng bị bỏ qua.</summary>
public sealed record ReorderFavoritesCommand(List<Guid> Ids) : IRequest<Unit>;

public sealed class ReorderFavoritesCommandValidator : AbstractValidator<ReorderFavoritesCommand>
{
    public ReorderFavoritesCommandValidator()
    {
        RuleFor(x => x.Ids).NotNull().WithMessage(ValidationMessages.Required);
        RuleFor(x => x.Ids).Must(ids => ids is null || ids.Count <= 500 && ids.Distinct().Count() == ids.Count)
            .WithMessage("Danh sách không hợp lệ.");
    }
}

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
