namespace Contract;

/// <param name="TotalMl">Tổng nước uống ghi trong ngày (ml); cũng là DayLogResponse.WaterMl và cộng vào water_g của nutrient/overview.</param>
public sealed record DayWaterResponse(DateOnly Date, decimal TotalMl, IReadOnlyList<WaterLogModel> Logs);
