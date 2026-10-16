namespace Domain;

// Snapshot nutrient bất biến tại thời điểm log (ADR-008); Amount null = chưa biết
public class FoodLogEntryNutrient : EntityBase<Guid>
{
    public Guid FoodLogEntryId { get; set; }
    public Guid NutrientDefinitionId { get; set; }
    public decimal? Amount { get; set; }
    public NutrientAmountStatus AmountStatus { get; set; } = NutrientAmountStatus.Unknown;

    public FoodLogEntry FoodLogEntry { get; set; } = null!;
    public NutrientDefinition Nutrient { get; set; } = null!;
}
