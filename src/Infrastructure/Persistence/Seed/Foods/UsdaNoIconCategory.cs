using System.Diagnostics;
using System.Text;

namespace Infrastructure;

public sealed record UsdaNoIconCategory(string Category, int Count, IReadOnlyList<string> Examples);
