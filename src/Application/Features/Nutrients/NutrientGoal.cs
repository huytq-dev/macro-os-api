namespace Application;

/// <summary>Mục tiêu hiệu lực của một chất: Floor/Target/Ceiling (null = không có), nguồn (<see cref="NutrientCatalog.Basis"/>) và loại (<see cref="NutrientGoals.Range"/>…).</summary>
public readonly record struct NutrientGoal(decimal? Floor, decimal? Target, decimal? Ceiling, string Basis, string Kind);
