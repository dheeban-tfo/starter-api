using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using starterapi.Models;

namespace starterapi.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    protected readonly TenantDbContext _context;
    private readonly DbSet<T> _dbSet;

    public GenericRepository(TenantDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<PagedResult<T>> GetPagedAsync(QueryParameters queryParameters, Expression<Func<T, bool>>? filter = null)
    {
        IQueryable<T> query = _dbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
        {
            var searchProperties = typeof(T).GetProperties()
                .Where(prop => Attribute.IsDefined(prop, typeof(SearchableAttribute)));

            query = query.Where(entity =>
                searchProperties.Any(prop =>
                    EF.Functions.Like(EF.Property<string>(entity, prop.Name), $"%{queryParameters.SearchTerm}%")
                )
            );
        }

        if (!string.IsNullOrEmpty(queryParameters.SortBy))
        {
            var sortProperty = typeof(T).GetProperty(queryParameters.SortBy);
            if (sortProperty != null)
            {
                var parameter = Expression.Parameter(typeof(T), "x");
                var property = Expression.Property(parameter, sortProperty);
                var lambda = Expression.Lambda<Func<T, object>>(Expression.Convert(property, typeof(object)), parameter);

                query = queryParameters.SortOrder.ToLower() == "desc"
                    ? query.OrderByDescending(lambda)
                    : query.OrderBy(lambda);
            }
        }

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();

        return new PagedResult<T>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize
        };
    }
}

// Custom attribute to mark searchable properties
[AttributeUsage(AttributeTargets.Property)]
public class SearchableAttribute : Attribute { }