namespace Application;

/// <summary>Cài đặt trang Food Log của user; chưa lưu lần nào thì trả mặc định (không ghi DB).</summary>
public sealed record GetFoodLogSettingsQuery : IRequest<FoodLogSettingsModel>;
