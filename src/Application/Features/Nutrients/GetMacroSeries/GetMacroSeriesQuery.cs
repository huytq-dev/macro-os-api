namespace Application;

/// <summary>
/// Biểu đồ Macros của trang Nutrition: một lần gọi trả P / F / C / kcal / Other theo từng ngày liên tục từ From đến To (To mặc định và tối đa hôm nay local), thống kê trung bình mỗi ngày đã log và bucket Day/Week/Month.
/// From mặc định 90 ngày trước To; All = true và không From: từ ngày log đầu tiên (tối đa 3660 ngày lùi từ To).
/// </summary>
public sealed record GetMacroSeriesQuery(DateOnly? From, DateOnly? To, bool? All = null, MetricGranularity? Granularity = null) : IRequest<MacroSeriesResponse>;
