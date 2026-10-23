namespace Domain;

// Lịch sử mục tiêu cân nặng; mục tiêu cũ giữ nguyên khi có goal mới (không ghi đè)
public class Goal : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public GoalType Type { get; set; }
    public decimal? TargetWeightKg { get; set; }    // nguồn duy nhất của cân nặng mục tiêu (MacroProgram.TargetWeightKg chỉ đồng bộ khi tạo)
    public decimal? StartWeightKg { get; set; }     // trend weight tại StartDate; Goal Progress % = (Start − trend) ÷ (Start − Target)
    public decimal? StartScaleWeightKg { get; set; } // cân thô tại StartDate (cùng lần cân với StartWeightKg); NULL cho goal có trước S28
    public GoalTrackingMode TrackingMode { get; set; } = GoalTrackingMode.Trend; // goal cũ = Trend (giữ hành vi); chỉ đổi khi user chủ động
    public decimal? RatePerWeekKg { get; set; }     // âm = giảm, dương = tăng
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public GoalStatus Status { get; set; } = GoalStatus.Active;
    public byte[] RowVersion { get; set; } = null!;

    public User User { get; set; } = null!;
}
