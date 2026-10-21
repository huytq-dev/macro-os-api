namespace Application;

/// <summary>
/// WP6 / UC-D06 Smart history: món user hay log quanh giờ <c>Time</c> (±90 phút, mặc định giờ hiện tại) và đúng thứ <c>Weekday</c> (bỏ qua = mọi thứ)
/// trong 90 ngày gần nhất, nhiều lần nhất trước. Không gồm quick add.
/// </summary>
public sealed record GetFoodSuggestionsQuery(TimeOnly? Time, DayOfWeek? Weekday, int Top = 8) : IRequest<IReadOnlyList<FoodSuggestionModel>>;
