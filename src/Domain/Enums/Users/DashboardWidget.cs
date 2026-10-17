namespace Domain;

// Mỗi giá trị = một tile/trang trên Dashboard; section suy từ key ở code, không lưu
public enum DashboardWidget
{
    // Carousel
    WeeklyNutrition, EnergyBalance, DailyNutrition,
    // Insights & Analytics
    Expenditure, WeightTrend, EnergyBalanceSummary, GoalProgress,
    // Habits
    WeighInHabit, FoodLoggingHabit,
    // Body Metrics
    ScaleWeight, VisualBodyFat,
    // Nutrition
    Calories, Protein, Fat, Carbs,
    // General
    Steps, Period
}
