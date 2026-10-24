namespace Application;

/// <summary>
/// MỘT định nghĩa "hôm nay" theo múi giờ hồ sơ của user: ngày/giờ hiện tại, ngày đầu tuần, múi giờ thực dùng và lý do dùng múi giờ dự phòng.
/// <para>Chính sách múi giờ thiếu/không hợp lệ (RÕ RÀNG, không âm thầm): dự phòng UTC, <see cref="Fallback"/> = "Missing" | "Invalid" và API hồ sơ trả <c>timeZoneFallback</c> để FE nhắc user chọn múi giờ; mọi ngày nghiệp vụ lấy từ đây.</para>
/// <para>UTC chỉ dùng cho dấu thời gian/audit/token, KHÔNG dùng cho ngày nghiệp vụ (log, cân, check-in, program, snapshot).</para>
/// </summary>
/// <param name="ZoneId">Múi giờ thực sự dùng (id của hồ sơ khi hợp lệ, ngược lại "UTC").</param>
/// <param name="Fallback">null = dùng đúng múi giờ hồ sơ; "Missing" = hồ sơ chưa đặt; "Invalid" = id không tồn tại.</param>
public sealed record UserLocale(DateOnly Today, TimeOnly Time, DayOfWeek WeekStart, string ZoneId = "UTC", string? Fallback = null)
{
    /// <summary>Tìm múi giờ theo id (IANA hoặc Windows). Id rỗng ⇒ Missing; không tồn tại ⇒ Invalid; cả hai rơi về UTC.</summary>
    public static (TimeZoneInfo Zone, string ZoneId, string? Fallback) ResolveZone(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId)) return (TimeZoneInfo.Utc, "UTC", "Missing");
        try { return (TimeZoneInfo.FindSystemTimeZoneById(timeZoneId.Trim()), timeZoneId.Trim(), null); }
        catch (TimeZoneNotFoundException) { return (TimeZoneInfo.Utc, "UTC", "Invalid"); }
        catch (InvalidTimeZoneException) { return (TimeZoneInfo.Utc, "UTC", "Invalid"); }
    }

    /// <summary>Hàm thuần: ngày/giờ local tại thời điểm <paramref name="utcNow"/> theo múi giờ <paramref name="timeZoneId"/> (xử lý DST theo quy tắc của múi giờ).</summary>
    public static UserLocale At(string? timeZoneId, DateTimeOffset utcNow, DayOfWeek weekStart)
    {
        var (zone, zoneId, fallback) = ResolveZone(timeZoneId);
        var local = TimeZoneInfo.ConvertTime(utcNow, zone);
        return new UserLocale(DateOnly.FromDateTime(local.DateTime), new TimeOnly(local.Hour, local.Minute), weekStart, zoneId, fallback);
    }

    // Hồ sơ (múi giờ, đầu tuần) chỉ đọc MỘT lần mỗi scope (request/job) trong tối đa 1 phút: một lệnh ghi trước đây đọc cùng dòng 3 lần (handler, ResolveTime, DerivedDataSync) = 3 round-trip tới DB.
    // Chỉ nhớ dữ liệu hồ sơ; "hôm nay" luôn tính lại từ đồng hồ nên qua nửa đêm vẫn đúng. Đổi hồ sơ trong cùng scope phải gọi Forget (UpdateProfile).
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<IUnitOfWork, Dictionary<Guid, (string? TimeZone, DayOfWeek WeekStart, DateTimeOffset At)>> Memos = new();
    private static readonly TimeSpan MemoTtl = TimeSpan.FromMinutes(1);

    public static async Task<UserLocale> GetAsync(IUnitOfWork uow, Guid userId, TimeProvider tp, CancellationToken ct)
    {
        var now = tp.GetUtcNow();
        var memo = Memos.GetOrCreateValue(uow);
        lock (memo)
            if (memo.TryGetValue(userId, out var m) && now - m.At < MemoTtl && now >= m.At) return At(m.TimeZone, now, m.WeekStart);

        var profile = await uow.Repository<UserProfile>().Query()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.TimeZone, p.WeekStartDay })
            .FirstOrDefaultAsync(ct);
        var weekStart = profile?.WeekStartDay ?? DayOfWeek.Monday;
        lock (memo) memo[userId] = (profile?.TimeZone, weekStart, now);
        return At(profile?.TimeZone, now, weekStart);
    }

    /// <summary>Quên hồ sơ đã nhớ của scope này (gọi sau khi đổi múi giờ/đầu tuần trong cùng request).</summary>
    public static void Forget(IUnitOfWork uow) => Memos.Remove(uow);

    /// <summary>Ngày ghi sau "hôm nay của user" bị từ chối (400), kể cả khi vẫn còn ≤ ngày UTC mai của validator tĩnh.</summary>
    public void EnsureNotFuture(DateOnly date, string field)
    {
        if (date > Today) throw new BadRequestException(ExceptionMessages.DateInFuture(field, Today));
    }
}
