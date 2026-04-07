// Domain/Entities/Nutrition/WaterLog.cs
using Domain;

public class WaterLog : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid FoodLogId { get; set; }
    public decimal AmountMl { get; set; }
    public DateTimeOffset LoggedAt { get; set; }
    public string? Source { get; set; }   // "Water", "Tea", "Juice"

    public User User { get; set; } = null!;
    public FoodLog FoodLog { get; set; } = null!;
}