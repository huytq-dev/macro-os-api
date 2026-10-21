using System.Diagnostics;
using System.Text;

namespace Infrastructure;

public sealed record UsdaBackfillReport(
    IReadOnlyDictionary<string, int> ReadByDataset, UsdaBackfillGroup UsdaFoods, UsdaBackfillGroup CatalogFoods, int ApiRequests, int RowsFromApi,
    IReadOnlyList<UsdaBackfillNoFdc> NoFdc, UsdaBackfillCoverage CatalogCoverage, UsdaBackfillCoverage UsdaCoverage, int RowsInserted, TimeSpan Elapsed, bool Applied, string? ApiError,
    int MacroFixes = 0, IReadOnlyList<string>? MacroFixSamples = null);
