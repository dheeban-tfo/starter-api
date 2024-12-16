using Microsoft.EntityFrameworkCore;
using starterapi.Models;
using starterapi.Repositories;
using starterapi.Services;

namespace starterapi;

public class UserRepository : BaseRepository<User>, IUserRepository
{
  

    public async Task DeactivateUserAsync(Guid id)
    {
        var user = await Context.Users.FindAsync(id);
        if (user != null)
        {
            user.IsActive = false;
            await Context.SaveChangesAsync();
        }
    }

   // private readonly TenantDbContext _context;

   public UserRepository(ITenantDbContextAccessor contextAccessor) : base(contextAccessor)
    {
    }

    public async Task<User> GetUserByIdAsync(Guid id)
    {
        return await Context.Users.FindAsync(id);
    }

    public async Task<IEnumerable<User>> GetUsersAsync()
    {
        return await Context.Users.ToListAsync();
    }

    public async Task AddUserAsync(User user)
    {
        await Context.Users.AddAsync(user);
        await Context.SaveChangesAsync();
    }

    public async Task UpdateUserAsync(User user)
    {
        Context.Users.Update(user);
        await Context.SaveChangesAsync();
    }

    public async Task<User> GetUserByEmailAsync(string email)
    {
        return await Context.Users.SingleOrDefaultAsync(u => u.Email == email) ?? throw new Exception("User not found.");
    }

    public async Task<PagedResult<User>> GetUsersAsync(QueryParameters queryParameters)
    {
         IQueryable<User> query = Context.Users;

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
