using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using PennyWise.Domain.Entities;
using PennyWise.Domain.Interfaces;
using PennyWise.Infrastructure.Data;

namespace PennyWise.Infrastructure.Repositories;

/// <summary>
/// Generic EF Core implementation of IRepository.
/// Provides CRUD operations against the PennyWiseDbContext.
/// </summary>
/// <typeparam name="T">Entity type inheriting from BaseEntity.</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly PennyWiseDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(PennyWiseDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id)
        => await _dbSet.FindAsync(id);

    public async Task<IEnumerable<T>> GetAllAsync()
        => await _dbSet.ToListAsync();

    public async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.Where(predicate).ToListAsync();

    public async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
        => await _dbSet.FirstOrDefaultAsync(predicate);

    public async Task AddAsync(T entity)
        => await _dbSet.AddAsync(entity);

    public void Update(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
    }

    public void Remove(T entity)
        => _dbSet.Remove(entity);

    public async Task<int> SaveChangesAsync()
        => await _context.SaveChangesAsync();
}
