namespace Contract;

/// <summary>
/// Nguyên liệu AI nhận diện: chỉ tên (tiếng Anh ngắn) và gram ước lượng, chưa có dinh dưỡng; FE gọi resolve-ingredient để tìm trong DB.
/// <paramref name="Quantity"/> + <paramref name="Unit"/> chỉ có khi món đếm được ("3" + "large"); Unit thuộc small | medium | large | extra large | slice | piece | cup | tbsp | tsp | bowl | plate.
/// <paramref name="State"/> chỉ có khi nhìn thấy/mô tả cách chế biến: raw | boiled | fried | scrambled | grilled | steamed | baked | roasted | stir_fried (không rõ = null). FE gửi nguyên 3 trường này lại cho resolve-ingredient.
/// </summary>
public sealed record AiIngredientModel(Guid Id, string Name, decimal Grams, decimal? Quantity = null, string? Unit = null, string? State = null);
