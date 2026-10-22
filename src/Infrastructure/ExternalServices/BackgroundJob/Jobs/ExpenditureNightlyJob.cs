namespace Infrastructure;

/// <summary>
/// Job chạy MỖI GIỜ (phút 30 UTC): dựng lại snapshot "hôm qua + hôm nay" cho các user có program đang ở giờ LOCAL 00 (<see cref="NightlyWindow"/>), tức mỗi user đúng một lần/ngày local (UC-F01/F02),
/// cùng đường có khoá user + job bền như mọi lần dựng khác. "Hôm qua/hôm nay" theo NGÀY LOCAL của từng user (múi giờ hồ sơ, DST/IANA qua UserLocale), không phải ngày UTC.
/// Idempotent (dựng lại là phép ghi đè cùng kết quả, dừng sớm khi hội tụ); lỗi một user không chặn user khác, phần chưa dựng thành job bền. Không có trạng thái lưu: lỡ một lần chạy thì lần ghi log kế tiếp tự dựng.
/// </summary>
[DisallowConcurrentExecution]
public sealed class ExpenditureNightlyJob(
    IUnitOfWork _uow,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider,
    ILogger<ExpenditureNightlyJob> _logger
) : IJob
{
    public const string CronUtc = "0 30 * * * ?";

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;

        var activeUserIds = await _uow.Repository<MacroProgram>().Query()
            .Where(p => p.IsActive)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(ct);

        // Một truy vấn lấy múi giờ; chỉ user có giờ local 00 ở lần chạy này mới được dựng (không dựng lại toàn bộ mỗi giờ)
        var zones = await _uow.Repository<UserProfile>().Query()
            .Where(p => activeUserIds.Contains(p.UserId))
            .Select(p => new { p.UserId, p.TimeZone })
            .ToDictionaryAsync(p => p.UserId, p => p.TimeZone, ct);
        var utcNow = _timeProvider.GetUtcNow();
        var userIds = activeUserIds.Where(id => NightlyWindow.IsDue(zones.GetValueOrDefault(id), utcNow)).ToList();

        foreach (var userId in userIds)
        {
            try
            {
                // Không giới hạn: dựng đủ hôm qua → hôm nay; không lấy được khoá/lỗi thì thành job bền (DerivedDataSync tự xếp)
                var today = (await UserLocale.GetAsync(_uow, userId, _timeProvider, ct)).Today;   // ngày local của user
                await DerivedDataSync.RunAsync(_uow, userId, today.AddDays(-1), today, null, RecalculationCause.Manual, _options.Value, _timeProvider, ct, bounded: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expenditure rebuild failed for user {UserId}", userId);
                _uow.DiscardChanges();
            }
        }
        _logger.LogInformation("Expenditure nightly job done for {Count} users", userIds.Count);
    }
}
