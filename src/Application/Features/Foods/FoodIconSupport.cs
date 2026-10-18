namespace Application;

/// <summary>
/// iconKey hiệu lực của user hiện tại: icon riêng user đã chọn (UserFoodIcon) nếu có, ngược lại icon của món (FoodItem.IconKey).
/// Mọi response có emoji đều trả iconKey qua đây; entry food log không snapshot icon mà đọc động, nên đổi icon thì lịch sử hiển thị theo icon mới.
/// </summary>
public static class FoodIconSupport
{
    /// <summary>Icon riêng của user cho các món (MỘT truy vấn cho cả lô); món không có dòng thì vắng mặt.</summary>
    public static async Task<Dictionary<Guid, string>> OverridesAsync(IUnitOfWork uow, Guid userId, IEnumerable<Guid> foodIds, CancellationToken ct)
    {
        var ids = foodIds.Distinct().ToList();
        if (ids.Count == 0) return [];
        return await uow.Repository<UserFoodIcon>().Query()
            .Where(i => i.UserId == userId && ids.Contains(i.FoodItemId))
            .ToDictionaryAsync(i => i.FoodItemId, i => i.IconKey, ct);
    }

    /// <summary>Icon hiệu lực của một món: override của user thắng icon của món.</summary>
    public static string? Effective(Guid? foodId, string? foodIconKey, IReadOnlyDictionary<Guid, string> overrides)
        => foodId is { } id && overrides.TryGetValue(id, out var own) ? own : foodIconKey;
}
