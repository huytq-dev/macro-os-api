namespace Contract;

/// <summary>Một chất của món, trên 100 g, theo <c>NutrientDefinition.Code</c> và đơn vị của chất đó (xem GET /nutrients?all=true).</summary>
public sealed record FoodNutrientInput(string Code, decimal AmountPer100g);
