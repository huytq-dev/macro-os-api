namespace Domain;

// FoodItem XOR Recipe, như FoodLogEntry
public class MealTemplateItem : EntityBase<Guid>
{
    public Guid MealTemplateId { get; set; }
    public Guid? FoodItemId { get; set; }
    public Guid? RecipeId { get; set; }
    public Guid? FoodServingId { get; set; }
    public decimal Quantity { get; set; }
    public MealType? MealType { get; set; }
    public int SortOrder { get; set; }

    public MealTemplate MealTemplate { get; set; } = null!;
    public FoodItem? FoodItem { get; set; }
    public Recipe? Recipe { get; set; }
    public FoodServing? FoodServing { get; set; }
}
