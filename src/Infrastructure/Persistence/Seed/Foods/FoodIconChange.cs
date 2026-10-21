using System.Diagnostics;
using System.Text;

namespace Infrastructure;

public sealed record FoodIconChange(string Old, string New, int Count, IReadOnlyList<string> Examples);
