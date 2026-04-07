// Domain/Entities/Nutrition/FoodItem.cs
using Domain;

public class FoodItem : EntityBase<Guid>, ISoftDeletable
{
    public string Name { get; set; } = null!;
    public string? Brand { get; set; }
    public string? Barcode { get; set; }            // EAN/UPC for scanning
    public FoodCategory Category { get; set; }
    public FoodSource Source { get; set; }          // System, UserCreated, API
    public Guid? CreatedByUserId { get; set; }
    public bool IsVerified { get; set; }
    public string? ImageUrl { get; set; }

    // Nutrition per 100g
    public decimal CaloriesPer100g { get; set; }
    public decimal ProteinPer100g { get; set; }
    public decimal CarbsPer100g { get; set; }
    public decimal FatPer100g { get; set; }
    public decimal? FiberPer100g { get; set; }
    public decimal? SugarPer100g { get; set; }
    public decimal? SodiumPer100g { get; set; }    // mg
    public decimal? PotassiumPer100g { get; set; } // mg
    public decimal? CalciumPer100g { get; set; }   // mg
    public decimal? IronPer100g { get; set; }      // mg
    public decimal? VitaminCPer100g { get; set; }  // mg
    public decimal? VitaminDPer100g { get; set; }  // µg
    public decimal? SaturatedFatPer100g { get; set; }
    public decimal? TransFatPer100g { get; set; }
    public decimal? CholesterolPer100g { get; set; } // mg

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Navigation
    public ICollection<FoodServing> Servings { get; set; } = [];
    public ICollection<FoodLogEntry> LogEntries { get; set; } = [];
    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
}
