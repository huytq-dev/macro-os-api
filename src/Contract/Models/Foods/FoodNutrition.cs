namespace Contract;

/// <summary>
/// Dinh dưỡng trên 100 g: kcal và g cho macro; mg cho sodium/potassium/calcium/iron/vitaminC/cholesterol; µg cho vitaminD.
/// Bốn macro lưu ở FoodItem, 11 trường còn lại lưu như mọi chất khác ở FoodNutrient (đọc/ghi qua đây chỉ để giữ contract cũ).
/// </summary>
public sealed record FoodNutrition(
    decimal CaloriesPer100g,
    decimal ProteinPer100g,
    decimal CarbsPer100g,
    decimal FatPer100g,
    decimal? FiberPer100g = null,
    decimal? SugarPer100g = null,
    decimal? SodiumPer100g = null,
    decimal? PotassiumPer100g = null,
    decimal? CalciumPer100g = null,
    decimal? IronPer100g = null,
    decimal? VitaminCPer100g = null,
    decimal? VitaminDPer100g = null,
    decimal? SaturatedFatPer100g = null,
    decimal? TransFatPer100g = null,
    decimal? CholesterolPer100g = null
);
