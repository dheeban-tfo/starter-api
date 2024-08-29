using System.Linq.Expressions;
using starterapi.Models;

namespace starterapi.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<PagedResult<T>> GetPagedAsync(QueryParameters queryParameters, Expression<Func<T, bool>>? filter = null);
    // Add other common methods here
}