namespace Domain;

public class MealTemplate : EntityBase<Guid>, ISoftDeletable
{
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public bool IsFavorite { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<MealTemplateItem> Items { get; set; } = [];
}
