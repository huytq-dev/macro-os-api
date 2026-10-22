namespace Infrastructure;

[RegisterService(typeof(IUserClock))]
public sealed class UserClock(IUnitOfWork _uow, IUserContext _userContext, TimeProvider _timeProvider) : IUserClock
{
    private UserLocale? _cached;
    private DateTimeOffset _cachedAt;

    public async Task<UserLocale> GetAsync(CancellationToken ct)
    {
        // Cache trong scope (một request); nếu scope sống lâu (job/stream) thì làm mới sau 1 phút để không giữ ngày cũ qua nửa đêm
        var now = _timeProvider.GetUtcNow();
        if (_cached is not null && now - _cachedAt < TimeSpan.FromMinutes(1)) return _cached;
        _cached = await UserLocale.GetAsync(_uow, _userContext.UserId, _timeProvider, ct);
        _cachedAt = now;
        return _cached;
    }

    public void Invalidate() => _cached = null;
}
