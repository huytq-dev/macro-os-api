namespace Domain;

// Kế hoạch ăn: planned khác consumed, chỉ tạo FoodLogEntry khi user bấm Log
public class MealPlan : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MealPlanStatus Status { get; set; } = MealPlanStatus.Draft;
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<PlannedMeal> Meals { get; set; } = [];
}
