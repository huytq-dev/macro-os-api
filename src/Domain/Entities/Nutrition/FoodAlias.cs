namespace Domain;

// Tên gọi khác / không dấu để search VI-EN
public class FoodAlias : EntityBase<Guid>
{
    public Guid FoodItemId { get; set; }
    public Guid? CanonicalFoodId { get; set; }      // alias của thực phẩm chuẩn (FoodItemId = dòng đại diện)
    public string Locale { get; set; } = null!;     // vi-VN, en-US
    public string Alias { get; set; } = null!;
    public string AliasNormalized { get; set; } = null!;    // lower + bỏ dấu

    public FoodItem FoodItem { get; set; } = null!;
}
