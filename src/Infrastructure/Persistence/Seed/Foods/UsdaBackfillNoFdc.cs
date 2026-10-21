using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Suggestions">Tối đa 3 món FDC gần tên nhất trong file local ("fdcId mô tả"), để người duyệt gắn bằng <c>verify-usda --fdc key=fdcId</c>; KHÔNG tự ghi.</param>
public sealed record UsdaBackfillNoFdc(string Key, string Name, string Provider, int Nutrients, IReadOnlyList<string> Suggestions);
