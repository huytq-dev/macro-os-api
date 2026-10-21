using System.Diagnostics;
using System.Text;

namespace Infrastructure;

/// <param name="Dir">Thư mục file JSON FDC đã giải nén (như import-usda-bulk).</param>
/// <param name="MaxRequests">Trần số request FDC cho món catalog đã gắn fdcId mà fdcId không nằm trong file local; 0 hoặc không có client = không gọi API.</param>
public sealed record UsdaBackfillOptions(bool Apply, string Dir, int BatchSize, int MaxRequests, int DelayMs);
