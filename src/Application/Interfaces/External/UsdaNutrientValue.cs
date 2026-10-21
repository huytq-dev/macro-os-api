namespace Application;

/// <param name="Number">Số chất của USDA (vd "203" = Protein), khớp NutrientDefinition.UsdaNumber.</param>
/// <param name="Unit">Đơn vị USDA gốc (g, mg, µg, kcal, IU, kJ).</param>
/// <param name="Amount">Lượng trên 100 g (Foundation / SR Legacy).</param>
public sealed record UsdaNutrientValue(string Number, string Unit, decimal Amount);
