namespace Domain;

// Override target của đúng 1 ngày; thắng MacroProgramWeek/DayTarget cùng ngày
public class NutritionTargetOverride : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public int CaloriesKcal { get; set; }
    public decimal ProteinG { get; set; }
    public decimal CarbsG { get; set; }
    public decimal FatG { get; set; }
    public string? Reason { get; set; }

    public User User { get; set; } = null!;
}
