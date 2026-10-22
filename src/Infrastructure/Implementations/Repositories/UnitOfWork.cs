using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

[RegisterService(typeof(IUnitOfWork))]
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext context;
    private readonly ConcurrentDictionary<Type, object> _repos = new();
    private IDbContextTransaction? _transaction;

    public UnitOfWork(ApplicationDbContext context, IDerivedDataScheduler derived, IReferenceCache cache)
    {
        this.context = context;
        Derived = derived;
        Cache = cache;
    }

    /// <summary>Không chạy nền, không cache (test/công cụ dòng lệnh dựng UnitOfWork thủ công): dữ liệu dẫn xuất dựng đồng bộ, luôn đọc DB.</summary>
    public UnitOfWork(ApplicationDbContext context) : this(context, NoDerivedDataScheduler.Instance, NoReferenceCache.Instance) { }

    public IDerivedDataScheduler Derived { get; }

    public IReferenceCache Cache { get; }

    public IGenericRepository<T> Repository<T>() where T : EntityBase<Guid>
        => (IGenericRepository<T>)_repos.GetOrAdd(typeof(T), _ => new GenericRepository<T>(context));

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);

    public async Task<bool> TryAcquireTransactionLockAsync(string resource, int timeoutMs, CancellationToken ct = default)
    {
        if (_transaction is null) throw new InvalidOperationException("Khoá theo transaction cần một transaction đang mở.");
        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await context.Database.ExecuteSqlRawAsync(
            "EXEC @result = sp_getapplock @Resource = @resource, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = @timeout",
            [result, new SqlParameter("@resource", resource), new SqlParameter("@timeout", timeoutMs)], ct);
        return result.Value is int code && code >= 0; // 0 = lấy ngay, 1 = lấy sau khi chờ; âm = timeout/bị huỷ/deadlock
    }

    public void DiscardChanges() => context.ChangeTracker.Clear();

    public async Task BeginTransactionAsync(CancellationToken ct = default)
        => _transaction = await context.Database.BeginTransactionAsync(ct);

    public async Task BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken ct = default)
        => _transaction = await context.Database.BeginTransactionAsync(isolationLevel, ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        context.Dispose();
    }
}
