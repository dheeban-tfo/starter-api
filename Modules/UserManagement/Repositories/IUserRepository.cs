using starterapi.Models;

namespace starterapi;

public interface IUserRepository
{
    Task<User> GetUserByIdAsync(Guid id);
    Task<IEnumerable<User>> GetUsersAsync();
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeactivateUserAsync(Guid id);
    Task<User> GetUserByEmailAsync(string email); 
    Task<PagedResult<User>> GetUsersAsync(QueryParameters queryParameters);
}
