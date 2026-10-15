namespace Domain;

// Audit trail cho mỗi lần algorithm điều chỉnh target tuần mới
public class WeeklyCheckIn : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly CheckInDate { get; set; }

    // Week mới được sinh ra từ check-in này
    public Guid MacroProgramWeekId { get; set; }

    // Snapshot lúc check-in (nullable: check-in đầu tiên hoặc Calibrating chưa có đủ data)
    public decimal TdeeEstimate { get; set; }
    public decimal? TrendWeightKg { get; set; }
    public decimal? WeightChangeRateKgPerWeek { get; set; }

    // Kết quả điều chỉnh
    public int PreviousCaloriesTarget { get; set; }
    public int NewCaloriesTarget { get; set; }
    public int CaloriesDelta { get; set; }               // = New - Previous

    public User User { get; set; } = null!;
    public MacroProgramWeek MacroProgramWeek { get; set; } = null!;
}
