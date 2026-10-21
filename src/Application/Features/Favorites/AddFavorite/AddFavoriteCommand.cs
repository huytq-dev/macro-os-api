namespace Application;

/// <summary>Lưu một preset yêu thích (tim ở chi tiết món). Cùng món/khẩu phần/số lượng đã có thì trả lại preset cũ (idempotent).</summary>
/// <param name="FoodServingId">Null = nhập theo gram (Quantity là gram).</param>
public sealed record AddFavoriteCommand(Guid FoodItemId, Guid? FoodServingId, decimal Quantity) : IRequest<Guid>;
