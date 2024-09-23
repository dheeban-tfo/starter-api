using System;
using System.Linq;
using System.Linq.Expressions;

namespace starterapi.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IDictionary<string, string> filters)
        {
            foreach (var filter in filters)
            {
                var property = typeof(T).GetProperty(filter.Key);
                if (property != null)
                {
                    var parameter = Expression.Parameter(typeof(T), "x");
                    var propertyAccess = Expression.Property(parameter, property);
                    var constant = Expression.Constant(Convert.ChangeType(filter.Value, property.PropertyType));
                    var condition = Expression.Call(propertyAccess, "Contains", Type.EmptyTypes, constant);
                    var lambda = Expression.Lambda<Func<T, bool>>(condition, parameter);
                    query = query.Where(lambda);
                }
            }
            return query;
        }
    }
}