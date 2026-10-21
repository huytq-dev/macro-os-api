namespace Application;

/// <summary>Kết quả đối chiếu kcal với macro; không bao giờ tự sửa dữ liệu nguồn.</summary>
public sealed record EnergyCheck(bool IsConsistent, decimal ExpectedKcal, decimal DifferenceKcal);
