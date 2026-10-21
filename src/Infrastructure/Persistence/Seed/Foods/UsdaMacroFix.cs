using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <summary>Món USDA nhập bằng mapper cũ có macro (protein/carbs/fat) = 0 dù FDC có số (Foundation ghi fat ở 298, carbs ở 205.2…): sửa đúng ô đang bằng 0, không đổi ô khác.</summary>
internal sealed record UsdaMacroFix(Guid FoodId, string Name, decimal? Protein, decimal? Carbs, decimal? Fat);
