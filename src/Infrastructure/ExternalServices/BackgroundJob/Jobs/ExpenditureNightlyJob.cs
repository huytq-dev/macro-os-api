namespace Infrastructure;

/// <summary>Job 00:30 UTC: dựng lại snapshot hôm qua + hôm nay cho mọi user có program (UC-F01/F02).</summary>
// ponytail: chạy theo UTC, không theo timezone user; đổi khi có user ngoài múi giờ VN
[DisallowConcurrentExecution]
public sealed class ExpenditureNightlyJob(
    IUnitOfWork _uow,
    IOptions<AlgorithmOptions> _options,
    TimeProvider _timeProvider,
    ILogger<ExpenditureNightlyJob> _logger
) : IJob
{
    public const string CronUtc = "0 30 0 * * ?";

    public async Task Execute(IJobExecutionContext context)
    {
        var ct = context.CancellationToken;
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);

        var userIds = await _uow.Repository<MacroProgram>().Query()
            .Where(p => p.IsActive)
            .Select(p => p.UserId)
            .Distinct()
            .ToListAsync(ct);

        foreach (var userId in userIds)
        {
            try
            {
                await ExpenditureRebuilder.RebuildAsync(_uow, userId, today.AddDays(-1), today, _options.Value, ct);
                await _uow.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expenditure rebuild failed for user {UserId}", userId);
            }
        }
        _logger.LogInformation("Expenditure nightly job done for {Count} users", userIds.Count);
    }
}
