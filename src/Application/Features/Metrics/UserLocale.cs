namespace Application;

/// <summary>Ngày/giờ hiện tại và ngày đầu tuần theo hồ sơ của user (UserProfile.TimeZone sai/thiếu → UTC; WeekStartDay thiếu hồ sơ → thứ Hai). MỘT truy vấn.</summary>
public sealed record UserLocale(DateOnly Today, TimeOnly Time, DayOfWeek WeekStart)
{
    public static async Task<UserLocale> GetAsync(IUnitOfWork uow, Guid userId, TimeProvider tp, CancellationToken ct)
    {
        var profile = await uow.Repository<UserProfile>().Query()
            .Where(p => p.UserId == userId)
            .Select(p => new { p.TimeZone, p.WeekStartDay })
            .FirstOrDefaultAsync(ct);

        var zone = TimeZoneInfo.Utc;
        if (!string.IsNullOrWhiteSpace(profile?.TimeZone))
        {
            try { zone = TimeZoneInfo.FindSystemTimeZoneById(profile.TimeZone); }
            catch (TimeZoneNotFoundException) { }
            catch (InvalidTimeZoneException) { }
        }

        var local = TimeZoneInfo.ConvertTime(tp.GetUtcNow(), zone);
        return new UserLocale(DateOnly.FromDateTime(local.DateTime), new TimeOnly(local.Hour, local.Minute), profile?.WeekStartDay ?? DayOfWeek.Monday);
    }

    /// <summary>Ngày ghi sau "hôm nay của user" bị từ chối (400), kể cả khi vẫn còn ≤ ngày UTC mai của validator tĩnh.</summary>
    public void EnsureNotFuture(DateOnly date, string field)
    {
        if (date > Today) throw new BadRequestException(ExceptionMessages.DateInFuture(field, Today));
    }
}
