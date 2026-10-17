namespace Domain;

public class MacroProgram : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public ProgramType ProgramType { get; set; }
    public GoalType GoalType { get; set; }
    public DietStyle DietStyle { get; set; }

    // Rate mục tiêu lưu theo % cân nặng/tuần (như MacroFactor); dương = giảm, âm = tăng, 0 = giữ
    public decimal WeeklyWeightChangeTargetPercent { get; set; }
    // Quy ra kg/tuần tại thời điểm tạo/check-in (âm = giảm, dương = tăng) — giữ để chart
    public decimal WeeklyWeightChangeTargetKg { get; set; }

    // Protein không tự điều chỉnh theo tuần (ADR-005)
    public ProteinLevel ProteinLevel { get; set; } = ProteinLevel.Moderate;
    public decimal ProteinGPerKgBodyweight { get; set; }
    public decimal? FixedProteinGrams { get; set; }

    // Sàn kcal/ngày cho Coached; null = tắt
    public int? CalorieFloorKcal { get; set; }

    public DayOfWeek CheckInWeekday { get; set; }
    public DateOnly StartDate { get; set; }

    // Thuật toán expenditure bỏ qua dữ liệu trước ngày này (BR-11)
    public DateOnly ExpenditureStartDate { get; set; }
    // User nhập tay TDEE khởi điểm thay công thức (UC-B05)
    public int? ManualInitialTdeeKcal { get; set; }

    public bool IsActive { get; set; } = true;
    // Thông tin program & chính sách (master prompt §6.1); IsActive vẫn là cờ query nóng, Status là vòng đời đầy đủ
    public string? Name { get; set; }
    public string? Description { get; set; }
    public Guid? GoalId { get; set; }
    public NutritionProgramStatus Status { get; set; } = NutritionProgramStatus.Active;
    public ProgramSource Source { get; set; } = ProgramSource.Generated;
    public string? StrategyVersion { get; set; }
    public DateOnly? EndedAt { get; set; }
    public decimal? TargetWeightKg { get; set; }            // chỉ đồng bộ từ Goal.TargetWeightKg khi tạo program; nguồn là Goal
    public CalorieShiftMode CalorieShiftMode { get; set; } = CalorieShiftMode.Even;
    public decimal? CalorieShiftPercent { get; set; }       // % dịch sang ngày được chọn; tổng tuần giữ nguyên
    public DaysOfWeek CalorieShiftDays { get; set; } = DaysOfWeek.None;
    public byte[] RowVersion { get; set; } = null!;

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<MacroProgramWeek> Weeks { get; set; } = [];
    public ICollection<MacroProgramDayTarget> DayTargets { get; set; } = [];
}
