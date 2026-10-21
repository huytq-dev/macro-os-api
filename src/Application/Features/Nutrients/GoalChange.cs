namespace Application;

/// <summary>Một lần đổi mục tiêu tuỳ chỉnh có hiệu lực từ <paramref name="EffectiveFrom"/> (ngày local của user); cả ba null = "về mặc định" từ ngày đó.</summary>
public readonly record struct GoalChange(DateOnly EffectiveFrom, decimal? Floor, decimal? Target, decimal? Ceiling)
{
    public UserGoal ToUserGoal() => new(Floor, Target, Ceiling);
}
