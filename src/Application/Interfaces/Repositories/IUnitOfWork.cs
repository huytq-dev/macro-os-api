namespace Application;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : EntityBase<Guid>;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task      BeginTransactionAsync(CancellationToken ct = default);
    Task      BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken ct = default);
    Task      CommitTransactionAsync(CancellationToken ct = default);
    Task      RollbackTransactionAsync(CancellationToken ct = default);

    /// <summary>Dựng lại dữ liệu dẫn xuất (aggregate, snapshot expenditure) sau khi phản hồi đã trả về; xem <see cref="IDerivedDataScheduler"/>.</summary>
    IDerivedDataScheduler Derived { get; }

    /// <summary>Cache dữ liệu tham chiếu (mã chất, món hệ thống) để bớt vòng đi về tới DB ở xa; xem <see cref="IReferenceCache"/>.</summary>
    IReferenceCache Cache { get; }
}
