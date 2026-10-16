namespace Domain;

// Kết quả OCR/AI/barcode chưa phải nguồn sự thật tới khi user Confirmed
public class FoodScanAnalysis : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public ScanMode Mode { get; set; }
    public ScanStatus Status { get; set; } = ScanStatus.Pending;
    public string? StorageRef { get; set; }
    public string? Provider { get; set; }
    public string? ModelVersion { get; set; }
    public string? ResultJson { get; set; }
    public decimal? Confidence { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public User User { get; set; } = null!;
}
