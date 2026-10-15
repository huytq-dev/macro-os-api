namespace Application;

public interface IGenericRepository<T> where T : EntityBase<Guid>
{
    Task<T>  AddAsync(T entity, CancellationToken ct = default);
    Task     RemoveAsync(T entity, CancellationToken ct = default);
    Task     RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);

    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default,
                       params Expression<Func<T, object>>[] includes);
    Task<T?> FindTrackedAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllTrackedAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    Task<QueryResult<T>> GetPagedAsync(
        QueryInfo query,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken ct = default);

    IQueryable<T> Query(bool tracking = false);
}
