using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using System.Reflection;

namespace SylviaNG.Assets.SharedKernel.Pagination
{
    public static class PaginationExtensions
    {
        public static async Task<PagedResult<T>> ToPaginatedResultAsync<T>(
            this IQueryable<T> query,
            PagedRequest request,
            CancellationToken cancellationToken = default)
        {

            // Apply search filter
            if (!string.IsNullOrEmpty(request.SearchTerm) && request.SearchProperties?.Length > 0)
            {
                query = ApplySearch(query, request.SearchTerm, request.SearchProperties);
            }


            // Apply sorting
            if (!string.IsNullOrEmpty(request.SortBy) && !string.IsNullOrEmpty(request.SortDirection))
            {
                query = ApplySorting(query, request.SortBy, request.SortDirection);
            }

            // Get total count before pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var items = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<T>
            {
                Data = items,
                PageNumber = request.Page,
                PageSize = request.PageSize,
                TotalCount = totalCount
            };
        }


        private static IQueryable<T> ApplySearch<T>(IQueryable<T> query, string searchTerm, string[] searchProperties)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? combinedExpression = null;

            foreach (var propertyName in searchProperties)
            {
                var property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);

                if (property == null || property.PropertyType != typeof(string))
                    continue;

                var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) })!;
                var searchValue = Expression.Constant(searchTerm);
                var containsCall = Expression.Call(propertyAccess, containsMethod, searchValue);

                combinedExpression = combinedExpression == null
                    ? containsCall
                    : Expression.OrElse(combinedExpression, containsCall);
            }

            if (combinedExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }


        private static IQueryable<T> ApplySorting<T>(
            IQueryable<T> query,
            string sortBy,
            string sortDirection)
        {
            var parameter = Expression.Parameter(typeof(T), "x");
            var property = GetNestedProperty(typeof(T), sortBy);

            if (property == null)
                return query; // Return unsorted if property doesn't exist

            var propertyAccess = CreatePropertyExpression(parameter, sortBy);
            var lambda = Expression.Lambda(propertyAccess, parameter);

            var methodName = sortDirection == "asc" ? "OrderBy" : "OrderByDescending";
            var method = typeof(Queryable).GetMethods()
                .First(m => m.Name == methodName && m.GetParameters().Length == 2);

            var genericMethod = method.MakeGenericMethod(typeof(T), property.PropertyType);
            var result = genericMethod.Invoke(null, new object[] { query, lambda });
            return (IQueryable<T>)(result ?? query);
        }

        private static PropertyInfo? GetNestedProperty(Type type, string propertyPath)
        {
            var properties = propertyPath.Split('.');
            PropertyInfo? property = null;
            var currentType = type;

            foreach (var prop in properties)
            {
                property = currentType.GetProperty(prop, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property == null)
                    return null;
                currentType = property.PropertyType;
            }

            return property;
        }

        private static Expression CreatePropertyExpression(ParameterExpression parameter, string propertyPath)
        {
            var properties = propertyPath.Split('.');
            Expression propertyAccess = parameter;

            foreach (var property in properties)
            {
                var propInfo = propertyAccess.Type.GetProperty(property, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (propInfo == null)
                    throw new ArgumentException($"Property '{property}' not found on type '{propertyAccess.Type.Name}'.");
                propertyAccess = Expression.Property(propertyAccess, propInfo);
            }

            return propertyAccess;
        }
    }
}
