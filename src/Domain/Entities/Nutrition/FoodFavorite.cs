namespace Domain;

public class FoodFavorite : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid FoodItemId { get; set; }

    public User User { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
}
