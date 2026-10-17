namespace Domain;

// Tuỳ chỉnh Dashboard: thứ tự + ẩn/hiện từng widget. User chưa có dòng nào → BE trả danh sách mặc định (không ghi DB)
public class DashboardWidgetSetting : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DashboardWidget WidgetKey { get; set; }
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; }

    public User User { get; set; } = null!;
}
