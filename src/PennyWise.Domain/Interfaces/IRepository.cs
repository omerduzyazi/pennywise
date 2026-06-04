using System.Linq.Expressions;
using PennyWise.Domain.Entities;

namespace PennyWise.Domain.Interfaces;

/// <summary>
/// Generic repository interface defining standard CRUD operations.
/// Adheres to the Repository Pattern to decouple domain logic from data access.
/// </summary>
/// <typeparam name="T">Entity type inheriting from BaseEntity.</typeparam>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
    Task<int> SaveChangesAsync();
}
