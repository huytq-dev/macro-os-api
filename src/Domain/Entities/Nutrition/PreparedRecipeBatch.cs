namespace Domain;

// Mẻ nấu thực tế: yield thật của lần nấu này
public class PreparedRecipeBatch : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid RecipeVersionId { get; set; }
    public decimal ActualYieldGrams { get; set; }
    public DateTimeOffset PreparedAt { get; set; }

    public User User { get; set; } = null!;
    public RecipeVersion RecipeVersion { get; set; } = null!;
}
