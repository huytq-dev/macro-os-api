using System.Text.RegularExpressions;

namespace Application;

public sealed record UsdaBulkPlan(IReadOnlyList<UsdaBulkCandidate> Selected, IReadOnlyDictionary<UsdaSkipReason, int> Skipped, IReadOnlyDictionary<UsdaSkipReason, List<string>> Samples);
