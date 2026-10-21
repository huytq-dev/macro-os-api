using System.Text.RegularExpressions;

namespace Application;

/// <summary>Macro /100 g của USDA (null = USDA không có số liệu cho chất đó).</summary>
public sealed record UsdaMacros(decimal? Kcal, decimal? Protein, decimal? Carbs, decimal? Fat);
