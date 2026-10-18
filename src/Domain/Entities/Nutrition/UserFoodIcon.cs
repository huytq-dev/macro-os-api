namespace Domain;

/// <summary>Icon user chọn riêng cho một món (kể cả món hệ thống/USDA/OFF mà user không sửa được). Có dòng này thì iconKey trả cho user đó là IconKey của dòng, thay icon của món.</summary>
public class UserFoodIcon : EntityBase<Guid>
{
    public Guid UserId { get; set; }
    public Guid FoodItemId { get; set; }
    public string IconKey { get; set; } = null!;

    public User User { get; set; } = null!;
    public FoodItem FoodItem { get; set; } = null!;
}
