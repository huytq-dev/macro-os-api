namespace Contract;

/// <summary>Món gợi ý theo giờ/thứ từ lịch sử log của chính user.</summary>
/// <param name="TimesLogged">Số lần user log món này trong cửa sổ giờ/thứ đã hỏi (90 ngày gần nhất).</param>
/// <param name="LastServingId">Khẩu phần và số lượng của lần log gần nhất — FE log lại đúng như vậy bằng một chạm.</param>
public sealed record FoodSuggestionModel(
    FoodItemModel Food,
    int TimesLogged,
    Guid? LastServingId,
    decimal LastQuantity
);
