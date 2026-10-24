namespace Application;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository<T> Repository<T>() where T : EntityBase<Guid>;
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task      BeginTransactionAsync(CancellationToken ct = default);
    Task      BeginTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken ct = default);
    Task      CommitTransactionAsync(CancellationToken ct = default);
    Task      RollbackTransactionAsync(CancellationToken ct = default);

    /// <summary>
    /// true = có transaction mở bằng <see cref="BeginTransactionAsync(CancellationToken)"/> nhưng server đã kết thúc nó (deadlock victim, KILL, mất kết nối): lệnh sau sẽ chạy
    /// NGOÀI transaction (autocommit), nên không được thử lại phần việc của transaction đó.
    /// </summary>
    bool IsTransactionBroken { get; }

    /// <summary>
    /// Lấy khoá độc quyền theo tên (SQL Server <c>sp_getapplock</c>, gắn với transaction hiện tại: tự nhả khi commit/rollback; an toàn nhiều instance). Phải gọi sau <see cref="BeginTransactionAsync(CancellationToken)"/>.
    /// false = quá <paramref name="timeoutMs"/> mà chưa lấy được.
    /// </summary>
    Task<bool> TryAcquireTransactionLockAsync(string resource, int timeoutMs, CancellationToken ct = default);

    /// <summary>Bỏ mọi thay đổi chưa lưu đang được theo dõi (sau khi rollback), để lần SaveChanges kế tiếp không ghi lại phần đã thất bại.</summary>
    void DiscardChanges();

    /// <summary>Đánh thức worker dựng lại nền khi có job mới; xem <see cref="IDerivedDataScheduler"/>.</summary>
    IDerivedDataScheduler Derived { get; }

    /// <summary>Cache dữ liệu tham chiếu (mã chất, món hệ thống) để bớt vòng đi về tới DB ở xa; xem <see cref="IReferenceCache"/>.</summary>
    IReferenceCache Cache { get; }
}
