namespace Application;

/// <summary>Layout dashboard: dòng đã lưu theo SortOrder; widget chưa lưu nối cuối và hiển thị; chưa tuỳ chỉnh gì → mặc định theo thứ tự enum. Không ghi DB.</summary>
public sealed record GetDashboardLayoutQuery : IRequest<DashboardLayoutResponse>;
