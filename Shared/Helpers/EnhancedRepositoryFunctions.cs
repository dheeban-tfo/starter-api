using System.Linq.Expressions;
using System.Reflection;

namespace starterapi.Repositories;

public static class EnhancedRepositoryFunctions
{
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, string sortBy, string sortOrder)
    {
        if (string.IsNullOrEmpty(sortBy))
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var property = Expression.Property(parameter, sortBy);
        var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(property, typeof(object)), parameter);

        return sortOrder.ToLower() == "desc" 
            ? query.OrderByDescending(lambda) 
            : query.OrderBy(lambda);
    }

    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, string searchTerm, params string[] searchProperties)
    {
        if (string.IsNullOrEmpty(searchTerm) || searchProperties.Length == 0)
        {
            return query;
        }

        var parameter = Expression.Parameter(typeof(T), "x");
        var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
        var searchTermExpression = Expression.Constant(searchTerm.ToLower());

        Expression combinedExpression = null;

        foreach (var prop in searchProperties)
        {
            var property = Expression.Property(parameter, prop);
            var toLower = Expression.Call(property, "ToLower", null);
            var containsExpression = Expression.Call(toLower, containsMethod, searchTermExpression);

            combinedExpression = combinedExpression == null
                ? containsExpression
                : Expression.OrElse(combinedExpression, containsExpression);
        }

        var lambda = Expression.Lambda<Func<T, bool>>(combinedExpression, parameter);
        return query.Where(lambda);
    }
}