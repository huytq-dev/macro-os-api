namespace Application;

/// <summary>Macro + fiber + sodium của một entry (cột snapshot FoodLogEntry). Null = chưa có dữ liệu, khác 0.</summary>
public sealed record EntryMacros(decimal Calories, decimal Protein, decimal Carbs, decimal Fat, decimal? Fiber, decimal? Sodium);
