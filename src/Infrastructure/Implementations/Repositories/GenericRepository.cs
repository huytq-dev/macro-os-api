namespace Infrastructure;

public class GenericRepository<T>(ApplicationDbContext context) : IGenericRepository<T>
    where T : EntityBase<Guid>
{
    protected readonly DbSet<T> DbSet = context.Set<T>();

    public async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        return entity;
    }

    public Task RemoveAsync(T entity, CancellationToken ct = default)
    {
        DbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public Task RemoveRangeAsync(IEnumerable<T> entities, CancellationToken ct = default)
    {
        DbSet.RemoveRange(entities);
        return Task.CompletedTask;
    }

    public Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.AsNoTracking().FirstOrDefaultAsync(predicate, ct);

    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default,
        params Expression<Func<T, object>>[] includes)
    {
        IQueryable<T> q = DbSet.AsNoTracking();
        foreach (var inc in includes) q = q.Include(inc);
        return await q.FirstOrDefaultAsync(predicate, ct);
    }

    public Task<T?> FindTrackedAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.FirstOrDefaultAsync(predicate, ct);

    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => DbSet.AnyAsync(predicate, ct);

    public async Task<IReadOnlyList<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await DbSet.AsNoTracking().Where(predicate).ToListAsync(ct);

    public async Task<IReadOnlyList<T>> GetAllTrackedAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await DbSet.Where(predicate).ToListAsync(ct);

    public async Task<QueryResult<T>> GetPagedAsync(
        QueryInfo query,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        CancellationToken ct = default)
    {
        IQueryable<T> q = DbSet.AsNoTracking();
        if (filter is not null) q = q.Where(filter);
        if (orderBy is not null) q = orderBy(q);

        var totalCount = query.NeedTotalCount ? await q.CountAsync(ct) : (int?)null;
        var items = await q.Skip(query.Skip).Take(query.Top).ToListAsync(ct);

        return new QueryResult<T> { Items = items, TotalCount = totalCount };
    }

    public IQueryable<T> Query(bool tracking = false)
        => tracking ? DbSet : DbSet.AsNoTracking();
}
