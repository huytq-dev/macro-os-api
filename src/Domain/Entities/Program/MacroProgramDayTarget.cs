namespace Domain;

// Per-day macro override trong 1 program.
// Dùng cho training day vs rest day, hoặc fasting day.
// Nếu 1 ngày không có row → dùng CaloriesTarget từ MacroProgramWeek active.
public class MacroProgramDayTarget : EntityBase<Guid>
{
    public Guid MacroProgramId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    // true = fasting day, Calories/macros = 0 (nullable fields bên dưới bị ignore)
    public bool IsFastingDay { get; set; }
    public DayType DayType { get; set; } = DayType.Standard;
    public string? AdditionalTargetsJson { get; set; }      // fiber, sugar, sodium... theo NutrientDefinition.Code

    // null = dùng week default
    public int? CaloriesTarget { get; set; }
    public decimal? ProteinTarget { get; set; }   // grams/day
    public decimal? CarbsTarget { get; set; }     // grams/day
    public decimal? FatTarget { get; set; }       // grams/day

    public MacroProgram MacroProgram { get; set; } = null!;
}
