using System.Text.RegularExpressions;

namespace Application;

/// <param name="Percent">Độ lệch tương đối so với số USDA (0.12 = lệch 12 %).</param>
public sealed record UsdaDiff(string Field, decimal Ours, decimal Usda, decimal Percent);
