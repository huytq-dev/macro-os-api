namespace Application;

/// <summary>
/// UC-F07: tổng quan dinh dưỡng theo kỳ (today/week/month/quarter/year, kết thúc ở Date, mặc định hôm nay).
/// Today: tổng ngày + top 3 món đóng góp; kỳ khác: trung bình mỗi ngày đã log, target macro = trung bình target các ngày đã log.
/// Month/quarter/year đọc từ NutritionDayAggregate (WP5).
/// </summary>
public sealed record GetNutritionOverviewQuery(OverviewRange Range = OverviewRange.Today, DateOnly? Date = null)
    : IRequest<NutritionOverviewResponse>;
