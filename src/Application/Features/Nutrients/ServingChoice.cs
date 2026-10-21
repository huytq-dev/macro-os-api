namespace Application;

/// <summary>Khẩu phần đã chọn cho một khối lượng; ServingId null = log theo gram (quantity = gram, unitLabel "g").</summary>
public sealed record ServingChoice(Guid? ServingId, decimal Quantity, string UnitLabel, decimal WeightGrams);
