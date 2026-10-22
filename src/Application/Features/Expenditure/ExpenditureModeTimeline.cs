namespace Application;

/// <summary>
/// Hàm thuần cho lịch sử Static/Dynamic (<see cref="ExpenditureModePeriod"/>). Ngày luôn là ngày LOCAL của user (theo TimeZone hồ sơ), không phải UTC.
/// Chế độ tại ngày d = dòng có EffectiveFrom lớn nhất ≤ d; không có dòng = Dynamic. Mỗi ngày tối đa một dòng nên không có khoảng chồng nhau.
/// </summary>
public static class ExpenditureModeTimeline
{
    public static (ExpenditureMode Mode, int? StaticTdeeKcal) ModeAt(IEnumerable<ExpenditureModePeriod> periods, DateOnly date)
    {
        var p = periods.Where(x => x.EffectiveFrom <= date).MaxBy(x => x.EffectiveFrom);
        return p is null ? (ExpenditureMode.Dynamic, null) : (p.Mode, p.Mode == ExpenditureMode.Static ? p.StaticTdeeKcal : null);
    }

    /// <summary>Lỗi (null = hợp lệ) khi chuyển chế độ: chỉ có hiệu lực từ hôm nay (local), không viết lại quá khứ; Static cần giá trị &gt; 0, Dynamic không mang giá trị.</summary>
    public static string? Validate(ExpenditureMode mode, int? staticTdeeKcal, DateOnly effectiveFrom, DateOnly localToday)
    {
        if (effectiveFrom != localToday) return "Chế độ chỉ có hiệu lực từ hôm nay (ngày local của user); không đổi chế độ cho ngày đã qua hoặc ngày tương lai.";
        if (mode == ExpenditureMode.Static && staticTdeeKcal is not > 0) return "Static cần expenditure > 0 kcal.";
        if (mode == ExpenditureMode.Dynamic && staticTdeeKcal is not null) return "Dynamic không mang giá trị Static.";
        return null;
    }

    /// <summary>Áp một lần chuyển chế độ: ghi đè dòng của chính ngày đó nếu đã có (chuyển nhiều lần trong ngày = bản cuối thắng), ngược lại thêm dòng mới. Không đụng dòng của ngày khác.</summary>
    public static List<ExpenditureModePeriod> Apply(IEnumerable<ExpenditureModePeriod> periods, Guid userId, ExpenditureMode mode, int? staticTdeeKcal, DateOnly effectiveFrom)
    {
        var all = periods.ToList();
        var existing = all.FirstOrDefault(x => x.EffectiveFrom == effectiveFrom);
        var list = all.Where(x => x.EffectiveFrom != effectiveFrom).ToList();
        list.Add(new ExpenditureModePeriod { Id = existing?.Id ?? Guid.NewGuid(), UserId = userId, EffectiveFrom = effectiveFrom, Mode = mode, StaticTdeeKcal = mode == ExpenditureMode.Static ? staticTdeeKcal : null });
        return list;
    }
}
