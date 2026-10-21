using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Foods">Số món tính độ phủ.</param>
/// <param name="Now">Số món hiện có ≥ 20 chất (4 macro + dòng FoodNutrient có số).</param>
/// <param name="After">Số món có ≥ 20 chất sau backfill.</param>
public sealed record UsdaBackfillCoverage(int Foods, int Now, int After)
{
    public double NowPercent => Foods == 0 ? 0 : 100.0 * Now / Foods;
    public double AfterPercent => Foods == 0 ? 0 : 100.0 * After / Foods;
}
