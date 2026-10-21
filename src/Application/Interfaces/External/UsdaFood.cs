namespace Application;

public sealed record UsdaFood(
    int FdcId,
    string Description,
    string DataType,
    string? Category,
    IReadOnlyList<UsdaPortion> Portions,
    IReadOnlyList<UsdaNutrientValue> Nutrients
);
