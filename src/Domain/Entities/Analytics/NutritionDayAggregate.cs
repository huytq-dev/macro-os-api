namespace Domain;

// Projection tổng dinh dưỡng theo ngày, rebuild được từ FoodLogEntries (khác DailyProgressSnapshot: job cuối ngày)
public class NutritionDayAggregate : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly LocalDate { get; set; }
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbs { get; set; }
    public decimal Fat { get; set; }
    public decimal Fiber { get; set; }
    public string? NutrientsJson { get; set; }
    public DateTimeOffset LastCalculatedAt { get; set; }

    public User User { get; set; } = null!;
}
