using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace Pukar.Usermanagement.Domain.Repositories.Interface;

public interface IBaseRepository<T> where T : class
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
    void RemoveRange(IEnumerable<T> entities);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    IQueryable<T> GetQueryable();
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
