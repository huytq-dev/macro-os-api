namespace Application;

/// <summary>Nguyên liệu thô do model trả, chưa làm sạch. Quantity/Unit (đếm được: 3 large) và State (raw, fried…) là tuỳ chọn.</summary>
public sealed record AiDraftIngredient(string Name, decimal Grams, decimal? Quantity = null, string? Unit = null, string? State = null);
