using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Dir">Thư mục file FDC (tuỳ chọn): có thì món USDA được tra nhóm FDC của chúng để dùng icon fallback theo nhóm; không có thì chỉ dùng tên.</param>
public sealed record FoodIconReassignOptions(bool Apply, string? Dir, int BatchSize);
