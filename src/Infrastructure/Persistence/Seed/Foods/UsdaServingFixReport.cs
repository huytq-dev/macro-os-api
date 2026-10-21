using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Infrastructure;

/// <param name="UsdaFoods">Số món USDA (ProviderCode = usda) được quét.</param>
/// <param name="FoodsAffected">Số món có ít nhất một khẩu phần cần sửa.</param>
/// <param name="Deleted">Khẩu phần sai mà tên đúng đã có sẵn ở món (vd đã có "1 serving") và không ai tham chiếu: bị xoá cho khỏi trùng.</param>
/// <param name="Renamed">Khẩu phần tên toàn số ("1 90000", "1.90000") đổi thành "1 serving" (gram giữ nguyên); nếu "1 serving" đã có và khẩu phần này đang được tham chiếu thì đổi theo gram ("90 g").</param>
/// <param name="DefaultsReset">Món có khẩu phần mặc định là khẩu phần chung ("1 serving") nên chọn lại mặc định: ưu tiên cup, rồi khẩu phần đếm được (piece, slice, medium…), không thì "100 g".</param>
/// <param name="HundredGramCreated">Số khẩu phần "100 g" phải tạo mới vì món chưa có mà cần làm mặc định.</param>
/// <param name="RaccRenamed">Nhãn viết tắt "RACC" của Foundation đổi thành "serving" ("1 RACC" → "1 serving").</param>
/// <param name="CodeStripped">Nhãn bị ghép mã món USDA ("0.5 cup From 19211") bỏ phần mã → "0.5 cup".</param>
public sealed record UsdaServingFixReport(int UsdaFoods, int FoodsAffected, int Deleted, int Renamed, int DefaultsReset, int HundredGramCreated,
    IReadOnlyList<string> Samples, TimeSpan Elapsed, bool Applied, int RaccRenamed = 0, int CodeStripped = 0);
