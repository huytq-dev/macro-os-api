namespace Application;

/// <summary>
/// Trang chi tiết nutrient cho một ngày (mặc định hôm nay, không ở tương lai theo múi giờ user): lượng + mức hiệu lực + nguồn mục tiêu (Strategy/Custom/Default/None),
/// trung bình Past Week (7 ngày) / Past Month (30 ngày) kết thúc ở ngày đó (mỗi ngày đã log, không tính ngày chưa đủ), mọi Contributors giảm dần và Nutrient Timing theo giờ (24 khung). Mã không biết → 404.
/// </summary>
public sealed record GetNutrientDayDetailQuery(string Code, DateOnly? Date) : IRequest<NutrientDayDetailResponse>;
