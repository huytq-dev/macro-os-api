namespace Application;

/// <summary>
/// Mục tiêu tuỳ chỉnh THEO NGÀY: ngày d dùng lần đổi có EffectiveFrom lớn nhất ≤ d; lần đổi rỗng (reset về mặc định) hoặc chưa có lần đổi nào ≤ d → không có ghi đè (dùng seed/DRI).
/// Calories/P/F/C không dùng lịch sử này (target theo tuần program của ngày đó).
/// </summary>
public static class NutrientGoalHistory
{
    /// <param name="changesAscending">Các lần đổi của MỘT nutrient, tăng dần theo EffectiveFrom.</param>
    /// <returns>null = ngày đó không có ghi đè (mặc định).</returns>
    public static UserGoal? On(IReadOnlyList<GoalChange> changesAscending, DateOnly date)
    {
        GoalChange? latest = null;
        foreach (var c in changesAscending)
        {
            if (c.EffectiveFrom > date) break;
            latest = c;
        }
        return latest is { } l && !l.ToUserGoal().IsEmpty ? l.ToUserGoal() : null;
    }

    /// <summary>Tất cả lần đổi của user có hiệu lực tới <paramref name="upTo"/> (một truy vấn), nhóm theo nutrient, tăng dần theo ngày.</summary>
    public static async Task<Dictionary<Guid, List<GoalChange>>> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly upTo, CancellationToken ct)
        => (await uow.Repository<UserNutrientTargetHistory>().Query()
                .Where(h => h.UserId == userId && h.EffectiveFrom <= upTo)
                .OrderBy(h => h.EffectiveFrom)
                .Select(h => new { h.NutrientDefinitionId, h.EffectiveFrom, h.Floor, h.Target, h.Ceiling })
                .ToListAsync(ct))
            .GroupBy(h => h.NutrientDefinitionId)
            .ToDictionary(g => g.Key, g => g.Select(h => new GoalChange(h.EffectiveFrom, h.Floor, h.Target, h.Ceiling)).ToList());
}
