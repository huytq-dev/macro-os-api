namespace Infrastructure;

/// <summary>
/// Chạy nền việc dựng lại aggregate dinh dưỡng + snapshot expenditure (<see cref="IDerivedDataScheduler"/>). Mỗi user có tối đa MỘT worker chạy tại một thời điểm
/// (hai lần ghi liên tiếp không đua nhau chèn cùng snapshot); yêu cầu đến khi worker đang chạy được gộp lại (ngày sớm nhất, hợp các ngày) và xử lý ngay sau lượt hiện tại.
/// Mỗi lượt dùng scope + DbContext riêng (không HttpContext, nên CreatedBy của snapshot để trống). Lỗi được ghi log và thử lại một lần; dữ liệu nguồn không bao giờ bị ảnh hưởng.
/// </summary>
[RegisterService(typeof(IDerivedDataScheduler), ServiceLifetime.Singleton)]
public sealed class DerivedDataScheduler(IServiceScopeFactory scopes, IOptions<AlgorithmOptions> options, TimeProvider time, ILogger<DerivedDataScheduler> logger) : IDerivedDataScheduler
{
    private sealed class Pending
    {
        public DateOnly From;
        public HashSet<DateOnly>? Dates;
    }

    private readonly object _gate = new();
    private readonly Dictionary<Guid, Pending> _pending = [];
    private readonly HashSet<Guid> _running = [];

    public bool IsAvailable => options.Value.RebuildDerivedInBackground;

    public void Schedule(Guid userId, DateOnly fromDate, IReadOnlyCollection<DateOnly>? nutritionDates)
    {
        lock (_gate)
        {
            if (_pending.TryGetValue(userId, out var p))
            {
                if (fromDate < p.From) p.From = fromDate;
                if (nutritionDates is { Count: > 0 }) (p.Dates ??= []).UnionWith(nutritionDates);
            }
            else _pending[userId] = new Pending { From = fromDate, Dates = nutritionDates is { Count: > 0 } ? [.. nutritionDates] : null };

            if (!_running.Add(userId)) return; // worker của user đang chạy sẽ nhặt phần mới sau lượt hiện tại
        }
        _ = Task.Run(() => RunAsync(userId));
    }

    /// <summary>Chờ mọi việc đang xếp hàng/đang chạy xong (dùng trong test và khi tắt app).</summary>
    public async Task WaitForIdleAsync(CancellationToken ct = default)
    {
        while (true)
        {
            lock (_gate) if (_running.Count == 0 && _pending.Count == 0) return;
            await Task.Delay(10, ct);
        }
    }

    private async Task RunAsync(Guid userId)
    {
        while (true)
        {
            Pending job;
            lock (_gate)
            {
                if (!_pending.Remove(userId, out job!)) { _running.Remove(userId); return; }
            }

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    using var scope = scopes.CreateScope();
                    var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    if (job.Dates is { Count: > 0 }) await NutritionAggregates.RefreshAsync(uow, userId, job.Dates, time, CancellationToken.None);
                    await ExpenditureRebuilder.RebuildAsync(uow, userId, job.From, FoodLogSupport.Today(time), options.Value, CancellationToken.None);
                    await uow.SaveChangesAsync();
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Derived data rebuild failed for {UserId} from {From} (attempt {Attempt})", userId, job.From, attempt);
                    if (attempt == 1) await Task.Delay(1000);
                }
            }
        }
    }
}
