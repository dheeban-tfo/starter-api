using Microsoft.EntityFrameworkCore;
using starterapi.Services;
using StarterApi.Models;


namespace StarterApi.Repositories
{
    public class CommunityRepository : ICommunityRepository
    {
        private readonly ITenantDbContextAccessor _contextAccessor;

        public CommunityRepository(ITenantDbContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task<Community> GetByIdAsync(int id)
        {
            return await _contextAccessor.TenantDbContext.Communities
                .Include(c => c.Blocks)
                .ThenInclude(b => b.Floors)
                .ThenInclude(f => f.Units)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<List<Community>> GetAllAsync()
        {
            return await _contextAccessor.TenantDbContext.Communities
                .Include(c => c.Blocks)
                .ThenInclude(b => b.Floors)
                .ThenInclude(f => f.Units)
                .ToListAsync();
        }

        public async Task<Community> CreateAsync(Community community)
        {
            _contextAccessor.TenantDbContext.Communities.Add(community);
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return community;
        }

        public async Task<Community> UpdateAsync(Community community)
        {
            _contextAccessor.TenantDbContext.Entry(community).State = EntityState.Modified;
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return community;
        }

        public async Task DeleteAsync(int id)
        {
            var community = await _contextAccessor.TenantDbContext.Communities.FindAsync(id);
            if (community != null)
            {
                _contextAccessor.TenantDbContext.Communities.Remove(community);
                await _contextAccessor.TenantDbContext.SaveChangesAsync();
            }
        }
    }
}