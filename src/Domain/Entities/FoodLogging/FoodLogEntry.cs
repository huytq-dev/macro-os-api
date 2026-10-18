// Domain/Entities/Nutrition/FoodLogEntry.cs
using Domain;

public class FoodLogEntry : EntityBase<Guid>, ISoftDeletable
{
    public Guid FoodLogId { get; set; }
    public MealType MealType { get; set; }          // Breakfast, Lunch, Dinner, Snack
    public TimeOnly? EatenAt { get; set; }

    // Either FoodItem OR Recipe (not both)
    public Guid? FoodItemId { get; set; }
    public Guid? RecipeId { get; set; }
    public Guid? FoodServingId { get; set; }
    public Guid? FoodLogBatchId { get; set; }       // commit plate nguyên tử
    public Guid? RecipeVersionId { get; set; }
    public EntrySource Source { get; set; } = EntrySource.Search;
    public DateTimeOffset? LoggedAtUtc { get; set; }        // UTC; LogDate của FoodLog là ngày local
    public string? ServingSnapshotJson { get; set; }        // serving/đơn vị tại lúc log
    public decimal? AiConfidence { get; set; }
    public string? Notes { get; set; }
    public byte[] RowVersion { get; set; } = null!;
    public decimal Quantity { get; set; }           // Number of servings

    // Snapshot tại thời điểm log (để không bị ảnh hưởng nếu food thay đổi)
    public decimal Calories { get; set; }
    public decimal Protein { get; set; }
    public decimal Carbs { get; set; }
    public decimal Fat { get; set; }
    public decimal? Fiber { get; set; }
    public decimal? Sodium { get; set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public FoodLog FoodLog { get; set; } = null!;
    public FoodItem? FoodItem { get; set; }
    public Recipe? Recipe { get; set; }
    public FoodServing? FoodServing { get; set; }
}
