using Microsoft.EntityFrameworkCore;
using starterapi.Models;
using starterapi.Services;
using StarterApi.Models;
using starterapi.Extensions;
using starterapi.Repositories;

namespace StarterApi.Repositories
{
    public class BlockRepository : IBlockRepository
    {
        private readonly ITenantDbContextAccessor _contextAccessor;

        public BlockRepository(ITenantDbContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task<Block> GetByIdAsync(int id)
        {
            return await _contextAccessor.TenantDbContext.Blocks
                .Include(b => b.Floors)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<PagedResult<Block>> GetAllAsync(QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Blocks
                .Include(b => b.Community)
                .AsQueryable();

            query = query.ApplyFilters(queryParameters.Filters);

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(b => b.Name.Contains(queryParameters.SearchTerm));
            }

            query = query.ApplySort(queryParameters.SortBy, queryParameters.SortOrder);

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResult<Block>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize
            };
        }

        public async Task<Block> CreateAsync(Block block)
        {
            _contextAccessor.TenantDbContext.Blocks.Add(block);
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return block;
        }

        public async Task<Block> UpdateAsync(Block block)
        {
            _contextAccessor.TenantDbContext.Entry(block).State = EntityState.Modified;
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return block;
        }

        public async Task DeleteAsync(int id)
        {
            var block = await _contextAccessor.TenantDbContext.Blocks.FindAsync(id);
            if (block != null)
            {
                _contextAccessor.TenantDbContext.Blocks.Remove(block);
                await _contextAccessor.TenantDbContext.SaveChangesAsync();
            }
        }

        public async Task<PagedResult<Block>> GetByCommunityAsync(int communityId, QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Blocks
                .Where(b => b.CommunityId == communityId)
                .AsQueryable();

            query = query.ApplyFilters(queryParameters.Filters);

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(b => b.Name.Contains(queryParameters.SearchTerm));
            }

            query = query.ApplySort(queryParameters.SortBy, queryParameters.SortOrder);

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResult<Block>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize
            };
        }
    }
}