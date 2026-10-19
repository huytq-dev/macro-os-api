namespace Domain;

// Cấu hình nút "+" ở thanh nav (sheet Shortcuts + toolbar) của user, đồng bộ nhiều thiết bị. 1 dòng/user, tạo khi PUT lần đầu;
// GET chưa có dòng trả mặc định của BE (ShortcutCatalog). Model luôn ghi giá trị tường minh (không HasDefaultValue, quy ước S8).
public class UserShortcutSetting : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public ToolbarStyle ToolbarStyle { get; set; } = ToolbarStyle.SearchAndBarcode;

    // Danh sách CÓ THỨ TỰ đủ catalog: [{"key":"search","enabled":true,"color":null}, …]; key cố định phía BE, màu thuộc bảng 7 màu hoặc null
    public string ShortcutsJson { get; set; } = "[]";

    public User User { get; set; } = null!;
}
