using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Infrastructure;

public sealed record UsdaServingFixOptions(bool Apply, int BatchSize);
