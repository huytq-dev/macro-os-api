namespace Domain;

public class MacroProgram : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public ProgramType ProgramType { get; set; }

    // WeightLoss, WeightGain, MuscleGain, Maintenance...
    public GoalType GoalType { get; set; }

    public DietStyle DietStyle { get; set; }

    // Tốc độ thay đổi cân nặng mục tiêu mỗi tuần (âm = giảm, dương = tăng)
    public decimal WeeklyWeightChangeTargetKg { get; set; }

    // Protein preference — dùng để tính ProteinTarget trong MacroProgramWeek
    // Algorithm không tự điều chỉnh protein, chỉ điều chỉnh calories/carbs/fat
    public decimal ProteinGPerKgBodyweight { get; set; }    // e.g. 1.8, 2.2 g/kg
    public decimal? FixedProteinGrams { get; set; }         // nếu user override bằng con số cố định

    public DateOnly StartDate { get; set; }

    // Chỉ 1 program active/user tại một thời điểm (enforced bằng partial unique index)
    public bool IsActive { get; set; } = true;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<MacroProgramWeek> Weeks { get; set; } = [];
    public ICollection<MacroProgramDayTarget> DayTargets { get; set; } = [];
}
