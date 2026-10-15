using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure;

[RegisterService(typeof(IUnitOfWork))]
public sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    private readonly ConcurrentDictionary<Type, object> _repos = new();
    private IDbContextTransaction? _transaction;

    public IGenericRepository<T> Repository<T>() where T : EntityBase<Guid>
        => (IGenericRepository<T>)_repos.GetOrAdd(typeof(T), _ => new GenericRepository<T>(context));

    public Task<int> SaveChangesAsync(CancellationToken ct = default)
        => context.SaveChangesAsync(ct);

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
