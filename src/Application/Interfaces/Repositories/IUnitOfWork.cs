namespace Application;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : EntityBase<Guid>;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task      BeginTransactionAsync(CancellationToken ct = default);
    Task      BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken ct = default);
    Task      CommitTransactionAsync(CancellationToken ct = default);
    Task      RollbackTransactionAsync(CancellationToken ct = default);
}
