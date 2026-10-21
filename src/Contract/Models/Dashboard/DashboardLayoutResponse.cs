namespace Contract;

/// <param name="Widgets">Đủ mọi widget: dòng đã lưu theo SortOrder, widget chưa lưu nối cuối (visible).</param>
public sealed record DashboardLayoutResponse(IReadOnlyList<DashboardWidgetModel> Widgets);
