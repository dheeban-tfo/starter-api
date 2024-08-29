using Microsoft.EntityFrameworkCore;
using starterapi.Services;

namespace starterapi.Repositories;

public abstract class BaseRepository<T> where T : class
{
    protected readonly ITenantDbContextAccessor _contextAccessor;
    protected TenantDbContext Context => _contextAccessor.TenantDbContext;

    protected BaseRepository(ITenantDbContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    public virtual async Task<T> GetByIdAsync(int id)
    {
        return await Context.Set<T>().FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await Context.Set<T>().ToListAsync();
    }

    public virtual async Task AddAsync(T entity)
    {
        await Context.Set<T>().AddAsync(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task UpdateAsync(T entity)
    {
        Context.Set<T>().Update(entity);
        await Context.SaveChangesAsync();
    }

    public virtual async Task DeleteAsync(T entity)
    {
        Context.Set<T>().Remove(entity);
        await Context.SaveChangesAsync();
    }
}