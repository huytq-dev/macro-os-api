// Domain/Entities/Nutrition/FoodLog.cs
// 1 FoodLog = 1 ngày của 1 user
using Domain;

public class FoodLog : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly LogDate { get; set; }
    public string? Notes { get; set; }

    public byte[] RowVersion { get; set; } = null!;

    // Denormalized daily totals (recalculated on entry change)
    public decimal TotalCalories { get; set; }
    public decimal TotalProtein { get; set; }
    public decimal TotalCarbs { get; set; }
    public decimal TotalFat { get; set; }
    public decimal TotalFiber { get; set; }
    public decimal TotalWater { get; set; }     // ml

    public User User { get; set; } = null!;
    public ICollection<FoodLogEntry> Entries { get; set; } = [];
    public ICollection<WaterLog> WaterLogs { get; set; } = [];
}