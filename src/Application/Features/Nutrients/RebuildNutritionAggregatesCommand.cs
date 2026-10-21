namespace Application;

/// <summary>WP5: dựng lại NutritionDayAggregate của user trong khoảng ngày (mặc định 365 ngày gần nhất) — backfill dữ liệu cũ hoặc sau khi sửa danh mục chất. Trả số ngày có dữ liệu.</summary>
public sealed record RebuildNutritionAggregatesCommand(DateOnly? From, DateOnly? To) : IRequest<int>;
