namespace Contract;

/// <summary>iconKey hiệu lực của user hiện tại sau khi đổi icon (null = chưa có icon, FE dùng emoji).</summary>
public sealed record FoodIconResponse(string? IconKey);
