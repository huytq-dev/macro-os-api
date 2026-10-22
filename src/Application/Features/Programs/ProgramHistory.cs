namespace Application;

/// <summary>
/// Target của MỘT NGÀY LỊCH SỬ: tuần program hiệu lực tại đúng ngày đó, kể cả khi program đã kết thúc (Superseded/Archived) — không còn chỉ đọc program đang active.
/// Chỉ program đã xoá mềm bị loại. Ghi/"program hiện tại" vẫn dùng program active, không đi qua đây.
/// Tuần hiệu lực = tuần có <c>EffectiveFrom</c> lớn nhất ≤ ngày, trong các program còn phủ ngày đó (<c>EndedAt</c> null hoặc ≥ ngày); đồng hạng thì program bắt đầu muộn hơn thắng.
/// D2: target theo thứ lấy từ snapshot 7 ngày của chính phiên bản tuần hiệu lực (<see cref="MacroProgramWeekDayTarget"/>); tuần cũ trước D2 fallback về dòng ngày + shift HIỆN TẠI và gắn <c>DayTargetSource = LEGACY_UNVERIFIED</c> (không kiểm chứng được).
/// </summary>
public static class ProgramHistory
{
    /// <summary>Mọi program (đang chạy lẫn đã kết thúc) có thể phủ [from, to], kèm tuần + dòng ngày, không tracking. <paramref name="from"/> null = từ đầu.</summary>
    public static async Task<List<MacroProgram>> LoadAsync(IUnitOfWork uow, Guid userId, DateOnly? from, DateOnly to, CancellationToken ct)
        => await uow.Repository<MacroProgram>().Query()
            .Include(p => p.Weeks).ThenInclude(w => w.DayTargetSnapshots).Include(p => p.DayTargets)
            .Where(p => p.UserId == userId && !p.IsDeleted && p.StartDate <= to && (from == null || p.EndedAt == null || p.EndedAt >= from))
            .ToListAsync(ct);

    /// <summary>Target của MỘT ngày bằng đúng một truy vấn chọn tuần (cùng quy tắc <see cref="EffectiveOn"/>: EffectiveFrom lớn nhất ≤ ngày, program còn phủ ngày, đồng hạng thì program bắt đầu muộn hơn); đường nóng của Food Log.</summary>
    public static async Task<ProgramWeekModel?> TargetOnAsync(IUnitOfWork uow, Guid userId, DateOnly date, CancellationToken ct)
    {
        var week = await uow.Repository<MacroProgramWeek>().Query()
            .Include(w => w.DayTargetSnapshots)
            .Include(w => w.MacroProgram).ThenInclude(p => p.DayTargets)
            .Where(w => w.MacroProgram.UserId == userId && !w.MacroProgram.IsDeleted && w.EffectiveFrom <= date
                && (w.MacroProgram.EndedAt == null || w.MacroProgram.EndedAt >= date))
            .OrderByDescending(w => w.EffectiveFrom).ThenByDescending(w => w.MacroProgram.StartDate).ThenByDescending(w => w.MacroProgram.CreatedAt)
            .FirstOrDefaultAsync(ct);
        return week is null ? null : ProgramTargetResolver.ForDate(week, week.MacroProgram, date);
    }

    /// <summary>Tuần (và program của nó) hiệu lực tại <paramref name="date"/>; null nếu chưa có program nào phủ ngày đó.</summary>
    public static (MacroProgram Program, MacroProgramWeek Week)? EffectiveOn(IEnumerable<MacroProgram> programs, DateOnly date)
    {
        (MacroProgram Program, MacroProgramWeek Week)? best = null;
        foreach (var program in programs)
        {
            if (program.IsDeleted || (program.EndedAt is { } end && date > end)) continue;
            foreach (var week in program.Weeks)
            {
                if (week.EffectiveFrom > date) continue;
                if (best is null || Beats(program, week, best.Value.Program, best.Value.Week)) best = (program, week);
            }
        }
        return best;
    }

    /// <summary>Provenance (VERIFIED | LEGACY_UNVERIFIED) của target ngày <paramref name="date"/>; null nếu không có tuần hiệu lực (chưa có target).</summary>
    public static string? ProvenanceOn(IEnumerable<MacroProgram> programs, DateOnly date)
        => EffectiveOn(programs, date) is { } e ? ProgramTargetResolver.ProvenanceOf(e.Week, date.DayOfWeek) : null;

    /// <summary>Target đã áp dịch kcal/dòng ngày của program đó cho <paramref name="date"/>; null nếu không có tuần hiệu lực.</summary>
    public static ProgramWeekModel? TargetOn(IEnumerable<MacroProgram> programs, DateOnly date)
        => EffectiveOn(programs, date) is { } e ? ProgramTargetResolver.ForDate(e.Week, e.Program, date) : null;

    private static bool Beats(MacroProgram program, MacroProgramWeek week, MacroProgram best, MacroProgramWeek bestWeek)
        => week.EffectiveFrom != bestWeek.EffectiveFrom ? week.EffectiveFrom > bestWeek.EffectiveFrom
         : program.StartDate != best.StartDate ? program.StartDate > best.StartDate
         : program.CreatedAt > best.CreatedAt;
}
