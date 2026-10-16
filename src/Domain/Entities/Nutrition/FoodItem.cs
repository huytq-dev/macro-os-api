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
    // Provenance & chất lượng dữ liệu (master prompt §5.4–5.5)
    public string? Country { get; set; }                    // ISO-2, vd VN
    public string? Language { get; set; }                   // vi-VN, en-US
    public string? ProviderCode { get; set; }               // usda, off, manual
    public string? ExternalFoodId { get; set; }
    public FoodQualityStatus QualityStatus { get; set; } = FoodQualityStatus.Unverified;
    public DateTimeOffset? SourceCapturedAt { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public string? OriginalUnits { get; set; }              // kJ, per serving... như trên nhãn gốc
    public bool IsUserEdited { get; set; }
    public decimal? DensityGPerMl { get; set; }             // cần để đổi ml <-> g, không dùng 1 giá trị chung
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;

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
