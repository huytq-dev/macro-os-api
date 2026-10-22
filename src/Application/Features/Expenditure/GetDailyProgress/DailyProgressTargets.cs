namespace Application;

/// <summary>
/// Target kcal của một ngày cho <c>GET /expenditure/daily</c>, tính KHI ĐỌC từ chương trình (cùng nguồn Food Log, Energy Balance, Nutrients, Check-in:
/// <see cref="ProgramHistory"/> + <see cref="ProgramTargetResolver"/>) thay vì cột <c>DailyProgressSnapshot.CaloriesTarget</c> lưu sẵn lúc dựng snapshot
/// (cột đó bị cũ sau check-in/đổi goal vì các handler đó không dựng lại snapshot). Cột vẫn được ghi nhưng không còn dùng để đọc.
/// </summary>
public static class DailyProgressTargets
{
    /// <summary>kcal target và id tuần hiệu lực của <paramref name="date"/>; (0, null) khi chưa có program nào phủ ngày đó (cùng quy ước 0 của snapshot cũ).</summary>
    public static (decimal Calories, Guid? WeekId) On(IReadOnlyList<MacroProgram> programs, DateOnly date)
        => ProgramHistory.EffectiveOn(programs, date) is { } e
            ? (ProgramTargetResolver.ForDate(e.Week, e.Program, date).CaloriesTarget, e.Week.Id)
            : (0m, null);
}
