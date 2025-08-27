using System.Linq.Expressions;
using Ardalis.Result;

namespace Ensemble.Maestro.Dotnet.Core.Data.Repositories;

/// <summary>
/// Generic repository interface for common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public interface IRepository<T> where T : class
{
    // Query operations
    Task<Result<T>> GetByIdAsync(Guid id);
    Task<Result<T>> GetByIdAsync(int id);
    Task<Result<IEnumerable<T>>> GetAllAsync();
    Task<Result<IEnumerable<T>>> GetAsync(Expression<Func<T, bool>> predicate);
    Task<Result<T>> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<Result<bool>> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task<Result<int>> CountAsync(Expression<Func<T, bool>>? predicate = null);

    // Query with includes
    Task<Result<T>> GetByIdWithIncludesAsync(Guid id, params Expression<Func<T, object>>[] includes);
    Task<Result<IEnumerable<T>>> GetWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    Task<Result<T>> GetFirstOrDefaultWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

    // Pagination
    Task<Result<(IEnumerable<T> Items, int TotalCount)>> GetPagedAsync<TKey>(
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        int page = 1,
        int pageSize = 10,
        params Expression<Func<T, object>>[] includes);

    // Modification operations
    Task<Result<T>> AddAsync(T entity);
    Task<Result<IEnumerable<T>>> AddRangeAsync(IEnumerable<T> entities);
    Task<Result<T>> UpdateAsync(T entity);
    Task<Result<IEnumerable<T>>> UpdateRangeAsync(IEnumerable<T> entities);
    Task<Result> DeleteAsync(T entity);
    Task<Result> DeleteAsync(Guid id);
    Task<Result> DeleteRangeAsync(IEnumerable<T> entities);
    Task<Result> DeleteRangeAsync(Expression<Func<T, bool>> predicate);

    // Bulk operations
    Task<Result<int>> BulkInsertAsync(IEnumerable<T> entities);
    Task<Result<int>> BulkUpdateAsync(IEnumerable<T> entities);
    Task<Result<int>> BulkDeleteAsync(Expression<Func<T, bool>> predicate);

    // Transaction support
    Task<Result<TResult>> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation);
    Task<Result> ExecuteInTransactionAsync(Func<Task> operation);

    // Save changes
    Task<Result<int>> SaveChangesAsync();
}