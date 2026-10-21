namespace Application;

/// <summary>Dinh dưỡng ước lượng cho TOÀN BỘ khối lượng đã hỏi (không phải /100 g).</summary>
public sealed record AiIngredientEstimate(decimal Calories, decimal Protein, decimal Fat, decimal Carbs);
