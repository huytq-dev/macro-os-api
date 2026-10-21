namespace Application;

/// <summary>Sắp xếp lại yêu thích theo thứ tự `Ids` (Library → Favorites → Reorder). Id không thuộc người dùng bị bỏ qua.</summary>
public sealed record ReorderFavoritesCommand(List<Guid> Ids) : IRequest<Unit>;
