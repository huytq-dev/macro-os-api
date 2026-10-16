// Định nghĩa các đơn vị serving của 1 food item
using Domain;

public class FoodServing : EntityBase<Guid>
{
    public Guid FoodItemId { get; set; }
    public string ServingName { get; set; } = null!;  // "1 cup", "1 slice", "100g"
    public decimal WeightInGrams { get; set; }
    public decimal? MillilitersEquivalent { get; set; }
    public ServingUnitType UnitType { get; set; } = ServingUnitType.Household;
    public decimal SizeValue { get; set; } = 1;             // 1 bát, 2 lát...
    public bool IsDefault { get; set; }

    public FoodItem FoodItem { get; set; } = null!;
    public ICollection<FoodLogEntry> LogEntries { get; set; } = [];
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
}