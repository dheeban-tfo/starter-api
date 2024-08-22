namespace starterapi;

public interface IUserRepository
{
    Task<User> GetUserByIdAsync(int id);
    Task<IEnumerable<User>> GetUsersAsync();
    Task AddUserAsync(User user);
    Task UpdateUserAsync(User user);
    Task DeactivateUserAsync(int id);
    Task<User> GetUserByEmailAsync(string email); 
}
