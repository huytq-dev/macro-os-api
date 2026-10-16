namespace Domain;

// Mỗi row = target của 1 tuần.
// Được tạo mới khi algorithm điều chỉnh (weekly check-in) hoặc user chỉnh tay.
public class MacroProgramWeek : EntityBase<Guid>
{
    public Guid MacroProgramId { get; set; }

    // Tuần này có hiệu lực từ ngày nào
    public DateOnly EffectiveFrom { get; set; }

    public int CaloriesTarget { get; set; }
    public decimal ProteinTarget { get; set; }   // grams/day
    public decimal CarbsTarget { get; set; }     // grams/day
    public decimal FatTarget { get; set; }       // grams/day

    public AdjustmentReason AdjustmentReason { get; set; }

    // Lưu delta để chart "thay đổi so với tuần trước"
    public int? PreviousCaloriesTarget { get; set; }

    public MacroProgram MacroProgram { get; set; } = null!;
    public WeeklyCheckIn? CheckIn { get; set; }
}
