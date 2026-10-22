namespace Domain;

// D2 (PCI-005): 7 target NGÀY HIỆU LỰC của một phiên bản MacroProgramWeek, chụp lúc phiên bản được ghi. Lịch sử đọc từ đây (theo ngày hiệu lực của tuần), KHÔNG đọc dòng MacroProgramDayTarget hiện tại.
// Bất biến: không bao giờ UPDATE/DELETE; mỗi lần ghi lại tuần (kể cả cùng ngày) thêm bộ 7 dòng mới với SnapshotRevision tăng dần, bộ cũ giữ làm audit (PCI-006).
// MacroProgramWeek.DaySnapshotRevision trỏ tới bộ hiện hành; null = tuần cũ trước D2 (LEGACY_UNVERIFIED, không backfill).
public class MacroProgramWeekDayTarget : EntityBase<Guid>
{
    public Guid MacroProgramWeekId { get; set; }
    public int SnapshotRevision { get; set; } = 1;
    public DayOfWeek DayOfWeek { get; set; }

    public bool IsFastingDay { get; set; }
    public bool IsLocked { get; set; }
    public DayType DayType { get; set; } = DayType.Standard;
    public TargetDistribution Distribution { get; set; }
    public AdjustmentReason AdjustmentReason { get; set; }
    public string? AdditionalTargetsJson { get; set; }

    // Giá trị đã resolve (ngày nhịn ăn = 0, đã áp dịch kcal / dòng ngày), không còn "null = dùng target tuần"
    public int CaloriesTarget { get; set; }
    public decimal ProteinTarget { get; set; }
    public decimal CarbsTarget { get; set; }
    public decimal FatTarget { get; set; }

    public MacroProgramWeek MacroProgramWeek { get; set; } = null!;
}
