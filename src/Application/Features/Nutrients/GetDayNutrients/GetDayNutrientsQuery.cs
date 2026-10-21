namespace Application;

/// <summary>UC-D09: mọi nutrient của một ngày (mặc định hôm nay) kèm target hiệu lực và pin — dữ liệu cho trang Nutrition và các tile Dashboard.</summary>
public sealed record GetDayNutrientsQuery(DateOnly? Date, bool All = false) : IRequest<DayNutrientsResponse>;
