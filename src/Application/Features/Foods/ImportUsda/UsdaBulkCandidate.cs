using System.Text.RegularExpressions;

namespace Application;

/// <param name="Name">Tên hiển thị đã làm gọn; tên gốc USDA (Food.Description) được giữ làm alias khi khác.</param>
public sealed record UsdaBulkCandidate(UsdaFood Food, string Name, string Key);
