namespace Application;

/// <summary>Chuỗi ngày cân liên tiếp (thuần, không DB).</summary>
/// <param name="Current">Số ngày liên tiếp kết thúc hôm nay; hôm nay chưa cân thì tính đến hôm qua (chưa đứt chuỗi cho tới hết hôm nay); 0 nếu cả hôm nay lẫn hôm qua đều không cân.</param>
/// <param name="Longest">Chuỗi dài nhất từ trước tới nay.</param>
/// <param name="IncludesToday">true = hôm nay đã cân (chuỗi hiện tại đã gồm hôm nay); false = chuỗi hiện tại (nếu &gt; 0) sẽ đứt nếu hôm nay không cân.</param>
public readonly record struct WeighInStreaks(int Current, int Longest, bool IncludesToday);
