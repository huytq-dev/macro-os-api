namespace Application;

/// <summary>Danh sách preset yêu thích của người dùng theo thứ tự tự sắp xếp.</summary>
public sealed record GetFavoritesQuery : IRequest<IReadOnlyList<FavoriteFoodModel>>;
