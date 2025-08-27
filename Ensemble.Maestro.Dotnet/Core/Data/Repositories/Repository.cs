using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using Ardalis.Result;

namespace Ensemble.Maestro.Dotnet.Core.Data.Repositories;

/// <summary>
/// Generic repository implementation for common CRUD operations
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly MaestroDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(MaestroDbContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException(nameof(context), "Database context cannot be null");
        }
        _context = context;
        _dbSet = _context.Set<T>();
    }

    // Query operations
    public virtual async Task<Result<T>> GetByIdAsync(Guid id)
    {
        try
        {
            var entity = await _dbSet.FindAsync(id);
            return entity != null 
                ? Result.Success(entity) 
                : Result.NotFound($"Entity with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving entity by ID {id}: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<T>> GetByIdAsync(int id)
    {
        try
        {
            var entity = await _dbSet.FindAsync(id);
            return entity != null 
                ? Result.Success(entity) 
                : Result.NotFound($"Entity with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving entity by ID {id}: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<IEnumerable<T>>> GetAllAsync()
    {
        try
        {
            var entities = await _dbSet.ToListAsync();
            return Result.Success(entities.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving all entities: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<IEnumerable<T>>> GetAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            var entities = await _dbSet.Where(predicate).ToListAsync();
            return Result.Success(entities.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving entities with predicate: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<T>> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            var entity = await _dbSet.FirstOrDefaultAsync(predicate);
            return entity != null 
                ? Result.Success(entity) 
                : Result.NotFound("Entity not found matching the specified criteria");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving first entity with predicate: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<bool>> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            var exists = await _dbSet.AnyAsync(predicate);
            return Result.Success(exists);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error checking entity existence: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<int>> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            var count = predicate != null ? await _dbSet.CountAsync(predicate) : await _dbSet.CountAsync();
            return Result.Success(count);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error counting entities: {ex.Message}" }, null));
        }
    }

    // Query with includes
    public virtual async Task<Result<T>> GetByIdWithIncludesAsync(Guid id, params Expression<Func<T, object>>[] includes)
    {
        try
        {
            IQueryable<T> query = _dbSet;
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            var entity = await query.FirstOrDefaultAsync(GetIdPredicate(id));
            
            return entity != null 
                ? Result.Success(entity) 
                : Result.NotFound($"Entity with ID {id} not found");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving entity by ID {id} with includes: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<IEnumerable<T>>> GetWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        try
        {
            IQueryable<T> query = _dbSet;
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            var entities = await query.Where(predicate).ToListAsync();
            return Result.Success(entities.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving entities with includes: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<T>> GetFirstOrDefaultWithIncludesAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        try
        {
            IQueryable<T> query = _dbSet;
            query = includes.Aggregate(query, (current, include) => current.Include(include));
            var entity = await query.FirstOrDefaultAsync(predicate);
            
            return entity != null 
                ? Result.Success(entity) 
                : Result.NotFound("Entity not found matching the specified criteria");
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving first entity with includes: {ex.Message}" }, null));
        }
    }

    // Pagination
    public virtual async Task<Result<(IEnumerable<T> Items, int TotalCount)>> GetPagedAsync<TKey>(
        Expression<Func<T, bool>>? predicate = null,
        Expression<Func<T, TKey>>? orderBy = null,
        bool ascending = true,
        int page = 1,
        int pageSize = 10,
        params Expression<Func<T, object>>[] includes)
    {
        try
        {
            if (page <= 0) 
                return Result<(IEnumerable<T>, int)>.Invalid(new ValidationError 
                { 
                    Identifier = nameof(page), 
                    ErrorMessage = "Page number must be greater than 0" 
                });
            
            if (pageSize <= 0) 
                return Result<(IEnumerable<T>, int)>.Invalid(new ValidationError 
                { 
                    Identifier = nameof(pageSize), 
                    ErrorMessage = "Page size must be greater than 0" 
                });

            IQueryable<T> query = _dbSet;

            // Apply includes
            query = includes.Aggregate(query, (current, include) => current.Include(include));

            // Apply predicate
            if (predicate != null)
                query = query.Where(predicate);

            // Get total count before paging
            var totalCount = await query.CountAsync();

            // Apply ordering
            if (orderBy != null)
                query = ascending ? query.OrderBy(orderBy) : query.OrderByDescending(orderBy);

            // Apply paging
            var skip = (page - 1) * pageSize;
            var items = await query.Skip(skip).Take(pageSize).ToListAsync();

            return Result.Success((items.AsEnumerable(), totalCount));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error retrieving paged entities: {ex.Message}" }, null));
        }
    }

    // Modification operations
    public virtual async Task<Result<T>> AddAsync(T entity)
    {
        try
        {
            if (entity == null) 
                return Result<T>.Invalid(new ValidationError 
                { 
                    Identifier = nameof(entity), 
                    ErrorMessage = "Entity cannot be null" 
                });
            
            var entry = await _dbSet.AddAsync(entity);
            return Result.Success(entry.Entity);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error adding entity: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<IEnumerable<T>>> AddRangeAsync(IEnumerable<T> entities)
    {
        try
        {
            if (entities == null) 
                return Result<IEnumerable<T>>.Invalid(new ValidationError 
                { 
                    Identifier = nameof(entities), 
                    ErrorMessage = "Entities collection cannot be null" 
                });
            
            var entityList = entities.ToList();
            if (!entityList.Any()) 
                return Result<IEnumerable<T>>.Invalid(new ValidationError 
                { 
                    Identifier = nameof(entities), 
                    ErrorMessage = "Entities collection cannot be empty" 
                });
            
            await _dbSet.AddRangeAsync(entityList);
            return Result.Success(entityList.AsEnumerable());
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error adding entities: {ex.Message}" }, null));
        }
    }

    public virtual Task<Result<T>> UpdateAsync(T entity)
    {
        try
        {
            if (entity == null) return Task.FromResult(Result<T>.Invalid(new ValidationError { ErrorMessage = "Entity cannot be null" }));
            
            var entry = _dbSet.Update(entity);
            return Task.FromResult(Result.Success(entry.Entity));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<T>.Error(new ErrorList(new[] { $"Error updating entity: {ex.Message}" }, null)));
        }
    }

    public virtual Task<Result<IEnumerable<T>>> UpdateRangeAsync(IEnumerable<T> entities)
    {
        try
        {
            if (entities == null) return Task.FromResult(Result<IEnumerable<T>>.Invalid(new ValidationError { ErrorMessage = "Entities collection cannot be null" }));
            
            var entityList = entities.ToList();
            if (!entityList.Any()) return Task.FromResult(Result<IEnumerable<T>>.Invalid(new ValidationError { ErrorMessage = "Entities collection cannot be empty" }));
            
            _dbSet.UpdateRange(entityList);
            return Task.FromResult(Result.Success(entityList.AsEnumerable()));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result<IEnumerable<T>>.Error(new ErrorList(new[] { $"Error updating entities: {ex.Message}" }, null)));
        }
    }

    public virtual Task<Result> DeleteAsync(T entity)
    {
        try
        {
            if (entity == null) return Task.FromResult(Result.Invalid(new[] { new ValidationError { ErrorMessage = "Entity cannot be null" } }));
            
            _dbSet.Remove(entity);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Error(new ErrorList(new[] { $"Error deleting entity: {ex.Message}" }, null)));
        }
    }

    public virtual async Task<Result> DeleteAsync(Guid id)
    {
        try
        {
            var entityResult = await GetByIdAsync(id);
            if (!entityResult.IsSuccess) return Result.NotFound($"Entity with ID {id} not found");
            
            _dbSet.Remove(entityResult.Value);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error deleting entity by ID {id}: {ex.Message}" }, null));
        }
    }

    public virtual Task<Result> DeleteRangeAsync(IEnumerable<T> entities)
    {
        try
        {
            if (entities == null) return Task.FromResult(Result.Invalid(new[] { new ValidationError { ErrorMessage = "Entities collection cannot be null" } }));
            
            var entityList = entities.ToList();
            if (!entityList.Any()) return Task.FromResult(Result.Invalid(new[] { new ValidationError { ErrorMessage = "Entities collection cannot be empty" } }));
            
            _dbSet.RemoveRange(entityList);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Error(new ErrorList(new[] { $"Error deleting entities: {ex.Message}" }, null)));
        }
    }

    public virtual async Task<Result> DeleteRangeAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            var entities = await _dbSet.Where(predicate).ToListAsync();
            if (!entities.Any()) return Result.Success(); // Nothing to delete
            
            _dbSet.RemoveRange(entities);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error deleting entities with predicate: {ex.Message}" }, null));
        }
    }

    // Bulk operations
    public virtual async Task<Result<int>> BulkInsertAsync(IEnumerable<T> entities)
    {
        try
        {
            if (entities == null) return Result.Invalid(new ValidationError { ErrorMessage = "Entities collection cannot be null" });
            
            var entityList = entities.ToList();
            if (!entityList.Any()) return Result.Invalid(new ValidationError { ErrorMessage = "Entities collection cannot be empty" });
            
            await _dbSet.AddRangeAsync(entityList);
            var result = await SaveChangesAsync();
            return result.IsSuccess ? Result.Success(result.Value) : Result.Error(new ErrorList(result.Errors.ToArray(), null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error bulk inserting entities: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<int>> BulkUpdateAsync(IEnumerable<T> entities)
    {
        try
        {
            if (entities == null) return Result.Invalid(new ValidationError { ErrorMessage = "Entities collection cannot be null" });
            
            var entityList = entities.ToList();
            if (!entityList.Any()) return Result.Invalid(new ValidationError { ErrorMessage = "Entities collection cannot be empty" });
            
            _dbSet.UpdateRange(entityList);
            var result = await SaveChangesAsync();
            return result.IsSuccess ? Result.Success(result.Value) : Result.Error(new ErrorList(result.Errors.ToArray(), null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error bulk updating entities: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result<int>> BulkDeleteAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            var entities = await _dbSet.Where(predicate).ToListAsync();
            if (!entities.Any()) return Result.Success(0); // Nothing to delete
            
            _dbSet.RemoveRange(entities);
            var result = await SaveChangesAsync();
            return result.IsSuccess ? Result.Success(result.Value) : Result.Error(new ErrorList(result.Errors.ToArray(), null));
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error bulk deleting entities: {ex.Message}" }, null));
        }
    }

    // Transaction support
    public virtual async Task<Result<TResult>> ExecuteInTransactionAsync<TResult>(Func<Task<TResult>> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var result = await operation();
            await transaction.CommitAsync();
            return Result.Success(result);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result.Error(new ErrorList(new[] { $"Transaction failed: {ex.Message}" }, null));
        }
    }

    public virtual async Task<Result> ExecuteInTransactionAsync(Func<Task> operation)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await operation();
            await transaction.CommitAsync();
            return Result.Success();
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return Result.Error(new ErrorList(new[] { $"Transaction failed: {ex.Message}" }, null));
        }
    }

    // Save changes
    public virtual async Task<Result<int>> SaveChangesAsync()
    {
        try
        {
            var changes = await _context.SaveChangesAsync();
            return Result.Success(changes);
        }
        catch (Exception ex)
        {
            return Result.Error(new ErrorList(new[] { $"Error saving changes: {ex.Message}" }, null));
        }
    }

    // Helper method to create ID predicate dynamically
    private Expression<Func<T, bool>> GetIdPredicate(Guid id)
    {
        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, "Id");
        var constant = Expression.Constant(id);
        var equal = Expression.Equal(property, constant);
        return Expression.Lambda<Func<T, bool>>(equal, parameter);
    }
}