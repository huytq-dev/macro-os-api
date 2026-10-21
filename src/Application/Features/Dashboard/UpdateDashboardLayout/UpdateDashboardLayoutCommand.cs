namespace Application;

/// <summary>Thay toàn bộ layout dashboard (idempotent). Widget vắng mặt trong danh sách quay về mặc định; danh sách rỗng = reset hết.</summary>
public sealed record UpdateDashboardLayoutCommand(IReadOnlyList<DashboardWidgetItem> Widgets) : IRequest<Unit>;
