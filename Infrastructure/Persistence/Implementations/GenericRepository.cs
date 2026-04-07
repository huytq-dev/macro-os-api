using System.Linq.Expressions;

namespace Infrastructure;

public class GenericRepository<T> : IGenericRepository<T> where T : EntityBase<Guid>
{
    private readonly ApplicationDbContext _db;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(ApplicationDbContext db)
    {
        _db = db;
        _dbSet = db.Set<T>();
    }

    public async Task<T> GetByIdAsync(Guid id)
        => await _dbSet.FindAsync(id) ?? throw new NotFoundException($"{typeof(T).Name} không tìm thấy.");

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public IQueryable<T> GetQueryable() => _dbSet;

    public async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<T> entities)
        => await _dbSet.AddRangeAsync(entities);

    public void Update(T entity) => _dbSet.Update(entity);

    public async Task RemoveAsync(Guid id)
    {
        var entity = await _dbSet.FindAsync(id)
            ?? throw new NotFoundException($"{typeof(T).Name} không tìm thấy.");
        _dbSet.Remove(entity);
    }

    public void RemoveRange(IEnumerable<T> entities) => _dbSet.RemoveRange(entities);

    public async Task<bool> IsExistAsync(Guid id)
        => await _dbSet.AnyAsync(e => e.Id == id);

    public async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.AnyAsync(predicate);

    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.FirstOrDefaultAsync(predicate, ct);

    public async Task<T?> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken ct, params string[] includes)
    {
        var query = includes.Aggregate(_dbSet.AsQueryable(),
            (q, include) => q.Include(include));
        return await query.FirstOrDefaultAsync(predicate, ct);
    }

    public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => await _dbSet.AnyAsync(predicate, ct);
}
