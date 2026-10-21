namespace Application;

/// <param name="TdeeRaw">Ước lượng thô theo CICO ngược trong cửa sổ.</param>
public sealed record ExpenditureRaw(decimal TdeeRaw, decimal AvgKcalIn, decimal RateKgPerWeek, int LoggedDays);
