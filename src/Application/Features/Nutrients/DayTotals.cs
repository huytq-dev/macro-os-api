namespace Application;

/// <summary>Tổng ngày cộng từ snapshot đã lưu của các entry chưa xoá.</summary>
public sealed record DayTotals(decimal Calories, decimal Protein, decimal Carbs, decimal Fat, decimal Fiber);
