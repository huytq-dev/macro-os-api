namespace Infrastructure;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : EntityBase<Guid>;

    #region System Reposittory

    #endregion

    // Transaction 
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync(CancellationToken ct = default);
}
