using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Dir">Thư mục chứa file JSON đã giải nén của FDC (tìm đệ quy; nhận dạng theo khoá gốc FoundationFoods / SRLegacyFoods / SurveyFoods, không theo tên file).</param>
/// <param name="Only">foundation | sr | fndds (null = cả ba).</param>
/// <param name="AllNutrients">false = chỉ chất core (bộ hiển thị mặc định của trang Nutrition); true = cả chất mở rộng.</param>
public sealed record UsdaBulkOptions(bool Apply, string Dir, string? Only, int? Max, int BatchSize, bool AllNutrients);
