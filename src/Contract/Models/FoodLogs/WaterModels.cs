namespace Contract;

/// <param name="Time">Giờ ghi (giờ địa phương của ngày log, cùng quy ước với EatenAt của entry).</param>
/// <param name="Source">Nhãn tự do: Water, Tea, Juice… (null = Water).</param>
public sealed record WaterLogModel(Guid Id, decimal AmountMl, TimeOnly Time, string? Source);

/// <param name="TotalMl">Tổng nước uống ghi trong ngày (ml); cũng là DayLogResponse.WaterMl và cộng vào water_g của nutrient/overview.</param>
public sealed record DayWaterResponse(DateOnly Date, decimal TotalMl, IReadOnlyList<WaterLogModel> Logs);
