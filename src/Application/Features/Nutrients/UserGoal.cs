namespace Application;

/// <summary>Ghi đè của user: LUÔN dùng nguyên bộ (null = không có mức đó), không trộn từng trường với mặc định.</summary>
public readonly record struct UserGoal(decimal? Floor, decimal? Target, decimal? Ceiling)
{
    public bool IsEmpty => Floor is null && Target is null && Ceiling is null;
}
