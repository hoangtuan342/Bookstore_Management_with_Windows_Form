using BookStore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace TrangSach.DAO.Entities;

/// <summary>Common persistence operations shared by the entity-specific DAO classes.</summary>
public abstract class EntityDao<TEntity>(BookStoreDbContext context) where TEntity : class
{
    protected BookStoreDbContext Context { get; } = context;
    private DbSet<TEntity> Set => Context.Set<TEntity>();
    protected IQueryable<TEntity> Query => Context.Set<TEntity>();

    public Task<List<TEntity>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().ToListAsync(cancellationToken);

    public Task<List<TEntity>> GetWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);

    public Task<TEntity?> FindSingleAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.SingleOrDefaultAsync(predicate, cancellationToken);

    public Task<TEntity?> FindSingleReadOnlyAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(predicate, cancellationToken);

    public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.AnyAsync(predicate, cancellationToken);

    public Task<int> CountWhereAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken = default) =>
        Set.CountAsync(predicate, cancellationToken);

    public ValueTask<TEntity?> FindAsync(object[] keyValues, CancellationToken cancellationToken = default) =>
        Set.FindAsync(keyValues, cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public void Add(TEntity entity) => Set.Add(entity);

    public void AddRange(IEnumerable<TEntity> entities) => Set.AddRange(entities);

    public void Update(TEntity entity) => Set.Update(entity);

    public void Remove(TEntity entity) => Set.Remove(entity);

    public void RemoveRange(IEnumerable<TEntity> entities) => Set.RemoveRange(entities);

    public void Delete(TEntity entity) => Remove(entity);

    public void DeleteRange(IEnumerable<TEntity> entities) => RemoveRange(entities);

}
