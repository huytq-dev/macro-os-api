namespace Application;

/// <summary>Nguyên liệu user đã chọn/xác nhận cho một tên AI: cần FoodItemId hoặc CanonicalFoodId. State (tuỳ chọn) là state AI đã trả, nhớ riêng theo state ("egg" + fried ≠ "egg").</summary>
public sealed record AiConfirmedIngredient(string Name, Guid? FoodItemId, Guid? CanonicalFoodId, string? State = null);
