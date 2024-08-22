using Microsoft.EntityFrameworkCore;
using starterapi.Models;
using starterapi.Repositories;

namespace starterapi;

public class UserRepository : IUserRepository
{
    // Existing code...

    public async Task DeactivateUserAsync(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsActive = false;
            await _context.SaveChangesAsync();
        }
    }

    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetUserByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await _context.Users.SingleOrDefaultAsync(u => u.Email == email) ?? throw new Exception("User not found.");
    }

    public async Task<PagedResult<User>> GetUsersAsync(QueryParameters queryParameters)
    {
         IQueryable<User> query = _context.Users;

        if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
        {
            query = query.ApplySearch(queryParameters.SearchTerm, "FirstName", "LastName", "Email");
        }

        query = query.ApplySort(queryParameters.SortBy, queryParameters.SortOrder);

        var totalItems = await query.CountAsync();

        var items = await query
            .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
            .Take(queryParameters.PageSize)
            .ToListAsync();

        return new PagedResult<User>
        {
            Items = items,
            TotalItems = totalItems,
            PageNumber = queryParameters.PageNumber,
            PageSize = queryParameters.PageSize
        };
    }
}
