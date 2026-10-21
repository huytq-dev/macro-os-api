namespace Contract;

/// <param name="Goal">Decrease | Increase | Neutral: hướng Good theo program đang chạy của user (không có program = Neutral).</param>
public sealed record FullBodyCompareResponse(DateOnly DateA, DateOnly DateB, string Goal, IReadOnlyList<FullBodyCompareItem> Items);
