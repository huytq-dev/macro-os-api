namespace Contract;

/// <summary>Khoảng tốc độ cho phép (% cân nặng/tuần, độ lớn) theo hướng goal. Slow ≤ SlowMax &lt; Standard ≤ StandardMax &lt; Fast ≤ FastMax; vượt FastMax bị từ chối.</summary>
/// <param name="DefaultPercent">Giá trị gợi ý khi user chưa chọn (giữa dải Standard).</param>
public sealed record GoalRateBoundsModel(decimal SlowMaxPercent, decimal StandardMaxPercent, decimal FastMaxPercent, decimal DefaultPercent);
