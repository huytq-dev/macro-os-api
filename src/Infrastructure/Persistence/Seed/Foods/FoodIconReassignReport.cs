using System.Diagnostics;
using System.Text;

namespace Infrastructure;

public sealed record FoodIconReassignReport(int Foods, int Changed, int NowNull, int WasNull, IReadOnlyList<FoodIconChange> Changes, IReadOnlyList<FoodIconConcept> Concepts,
    int ConceptsTotal, int UserOverridesKept, TimeSpan Elapsed, bool Applied);
