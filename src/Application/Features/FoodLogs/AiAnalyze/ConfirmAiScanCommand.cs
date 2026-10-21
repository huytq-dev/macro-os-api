namespace Application;

/// <summary>
/// Khi user log món từ kết quả AI: chuyển FoodScanAnalysis Completed → Confirmed và nhớ lựa chọn từng nguyên liệu (AiIngredientCache, theo user)
/// để lần sau resolve-ingredient trả đúng món đó. Gọi lại với cùng scan thì cập nhật bộ nhớ (idempotent).
/// </summary>
public sealed record ConfirmAiScanCommand(Guid ScanId, List<AiConfirmedIngredient> Ingredients) : IRequest<AiScanConfirmResponse>;
