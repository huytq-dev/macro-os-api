namespace Domain;

// Lịch sử mục tiêu cân nặng; mục tiêu cũ giữ nguyên khi có goal mới (không ghi đè)
public class Goal : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public GoalType Type { get; set; }
    public decimal? TargetWeightKg { get; set; }
    public decimal? RatePerWeekKg { get; set; }     // âm = giảm, dương = tăng
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public byte[] RowVersion { get; set; } = null!;

    public User User { get; set; } = null!;
}
