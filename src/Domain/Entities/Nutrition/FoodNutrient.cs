namespace Domain;

// Giá trị trên 100g/100ml. AmountPer100g null = chưa biết, KHÔNG phải 0
public class FoodNutrient : EntityBase<Guid>
{
    public Guid FoodItemId { get; set; }
    public Guid NutrientDefinitionId { get; set; }
    public decimal? AmountPer100g { get; set; }
    public NutrientAmountStatus AmountStatus { get; set; } = NutrientAmountStatus.Unknown;

    public FoodItem FoodItem { get; set; } = null!;
    public NutrientDefinition Nutrient { get; set; } = null!;
}
