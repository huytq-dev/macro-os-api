namespace Domain;

// FoodItem XOR Recipe; LoggedFoodLogEntryId gán khi user xác nhận đã ăn
public class PlannedMeal : EntityBase<Guid>
{
    public Guid MealPlanId { get; set; }
    public DateOnly PlannedDate { get; set; }
    public MealType MealType { get; set; }
    public Guid? FoodItemId { get; set; }
    public Guid? RecipeId { get; set; }
    public Guid? FoodServingId { get; set; }
    public decimal Quantity { get; set; }
    public Guid? LoggedFoodLogEntryId { get; set; }

    public MealPlan MealPlan { get; set; } = null!;
    public FoodItem? FoodItem { get; set; }
    public Recipe? Recipe { get; set; }
    public FoodServing? FoodServing { get; set; }
}
