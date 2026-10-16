namespace Domain;

public class FoodImage : EntityBase<Guid>
{
    public Guid FoodItemId { get; set; }
    public string StorageKey { get; set; } = null!;
    public string? LicenseMetadata { get; set; }
    public bool IsPrimary { get; set; }

    public FoodItem FoodItem { get; set; } = null!;
}
