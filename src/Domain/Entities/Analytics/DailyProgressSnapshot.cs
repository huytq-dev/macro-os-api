// Domain/Entities/Analytics/DailyProgressSnapshot.cs
// Job chạy mỗi ngày, denormalize data để query nhanh
using Domain;

public class DailyProgressSnapshot : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public DateOnly SnapshotDate { get; set; }

    // Ngày có FoodLog hay không — phân biệt "chưa log" (UI: –/N/A) với log 0 kcal
    public bool IsLogged { get; set; }

    // Nutrition
    public decimal CaloriesConsumed { get; set; }
    public decimal CaloriesTarget { get; set; }
    public decimal ProteinConsumed { get; set; }
    public decimal CarbsConsumed { get; set; }
    public decimal FatConsumed { get; set; }
    public decimal WaterConsumed { get; set; }

    // Workout
    public int WorkoutCount { get; set; }
    public int TotalWorkoutMinutes { get; set; }
    public decimal CaloriesBurned { get; set; }

    // Activity
    public int? StepsCount { get; set; }      // từ HealthIntegration hoặc manual input

    // Body — trend weight từ WeightEntry của ngày hôm đó (null nếu không có)
    public decimal? TrendWeightKg { get; set; }

    // Week target đang active vào ngày này (để chart target vs actual chính xác)
    public Guid? MacroProgramWeekId { get; set; }

    public User User { get; set; } = null!;
    public MacroProgramWeek? MacroProgramWeek { get; set; }
}