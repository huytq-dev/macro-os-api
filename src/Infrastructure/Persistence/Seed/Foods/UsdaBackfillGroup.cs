using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Foods">Số món thuộc nhóm.</param>
/// <param name="FoodsToUpdate">Số món sẽ được thêm ít nhất một dòng chất.</param>
/// <param name="Rows">Số dòng FoodNutrient sẽ thêm.</param>
/// <param name="NotFound">Số món có fdcId nhưng không lấy được dữ liệu (không có trong file local và không gọi được API).</param>
public sealed record UsdaBackfillGroup(int Foods, int FoodsToUpdate, int Rows, int NotFound, IReadOnlyList<string> NotFoundSamples);
