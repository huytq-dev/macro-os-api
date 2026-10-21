using System.Diagnostics;
using System.Text;

namespace Infrastructure;

public sealed record UsdaBulkReport(
    IReadOnlyDictionary<string, int> ReadByDataset, IReadOnlyList<string> IgnoredFiles, IReadOnlyDictionary<UsdaSkipReason, int> Skipped, IReadOnlyDictionary<UsdaSkipReason, List<string>> SkipSamples,
    int ToAdd, int WithIcon, IReadOnlyList<UsdaNoIconCategory> NoIcon, IReadOnlyDictionary<string, int> ToAddByDataset, int Servings, int Nutrients, int Aliases, int Inserted, TimeSpan Elapsed, bool Applied)
{
    public int Read => ReadByDataset.Values.Sum();
    public double IconPercent => ToAdd == 0 ? 0 : 100.0 * WithIcon / ToAdd;
}
