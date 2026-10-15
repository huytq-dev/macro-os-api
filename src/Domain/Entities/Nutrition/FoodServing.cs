// Định nghĩa các đơn vị serving của 1 food item
using Domain;

public class FoodServing : EntityBase<Guid>
{
    public Guid FoodItemId { get; set; }
    public string ServingName { get; set; } = null!;  // "1 cup", "1 slice", "100g"
    public decimal WeightInGrams { get; set; }
    public bool IsDefault { get; set; }

    public FoodItem FoodItem { get; set; } = null!;
    public ICollection<FoodLogEntry> LogEntries { get; set; } = [];
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
}