using System.Linq.Expressions;

namespace SylviaNG.Assets.SharedKernel.Generic
{
    /// <summary>
    /// Generic repository interface for basic CRUD and advanced data access operations.
    /// </summary>
    /// <typeparam name="T">Entity type</typeparam>
    public interface IRepository<T> where T : class
    {
        IQueryable<T> GetQueryable();
        Task<T?> GetByIdAsync(long id);
        Task<IEnumerable<T>> GetAllAsync();
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
        Task AddAsync(T entity);
        Task AddRangeAsync(IEnumerable<T> entities);
        void Update(T entity);
        void Delete(T entity);
        Task DeleteByIdAsync(long id);
        IQueryable<T> Query(bool asNoTracking = true);
        Task<T?> GetByIdWithIncludeAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task<List<T>> GetAllWithIncludeAsync(params Expression<Func<T, object>>[] includes);
        Task<List<T>> FindWithIncludeAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
        Task DeleteWhereAsync(Expression<Func<T, bool>> predicate);
        void DeleteRange(IEnumerable<T> entities);
    }
}
