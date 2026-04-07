using System.Linq.Expressions;

namespace Infrastructure;

public interface IGenericRepository<T> where T : EntityBase<Guid>
{
    Task<T> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    IQueryable<T> GetQueryable();
    Task<T> AddAsync(T entity);
    Task AddRangeAsync(IEnumerable<T> entities);
    void Update(T entity);
    Task RemoveAsync(Guid id);
    void RemoveRange(IEnumerable<T> entities);
    Task<bool> IsExistAsync(Guid id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    // Query helpers — Application dùng những method này, không cần EF Core trực tiếp
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct, params string[] includes);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
}
