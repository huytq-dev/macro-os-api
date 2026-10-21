namespace Application;

public sealed record UsdaMappedFood(FoodItem Food, IReadOnlyList<UsdaMappedNutrient> Nutrients);
