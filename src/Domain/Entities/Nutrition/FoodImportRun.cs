namespace Domain;

// Một lần nhập dữ liệu thực phẩm từ nguồn ngoài (USDA); theo dõi kết quả từng lần chạy
public class FoodImportRun : EntityBase<Guid>
{
    public string Provider { get; set; } = null!;   // usda
    public string? Query { get; set; }              // từ khoá tìm hoặc danh sách fdcId đã yêu cầu
    public ImportRunStatus Status { get; set; } = ImportRunStatus.Running;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? FinishedAt { get; set; }
    public int Imported { get; set; }               // món mới tạo
    public int Skipped { get; set; }                // món đã có (idempotent) hoặc bỏ qua vì thiếu dữ liệu
    public string? Error { get; set; }
}
