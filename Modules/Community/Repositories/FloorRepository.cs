using Microsoft.EntityFrameworkCore;
using starterapi.Models;
using starterapi.Services;
using StarterApi.Models;
using starterapi.Extensions;
using starterapi.Repositories;
using StarterApi.Models.Communities;

namespace StarterApi.Repositories
{
    public class FloorRepository : IFloorRepository
    {
        private readonly ITenantDbContextAccessor _contextAccessor;

        public FloorRepository(ITenantDbContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task<Floor> GetByIdAsync(int id)
        {
            return await _contextAccessor.TenantDbContext.Floors
                .Include(f => f.Units)
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        public async Task<PagedResult<Floor>> GetAllAsync(QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Floors
                .Include(f => f.Block)
                .AsQueryable();

            query = query.ApplyFilters(queryParameters.Filters);

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(f => f.FloorNumber.ToString().Contains(queryParameters.SearchTerm));
            }

            query = query.ApplySort(queryParameters.SortBy, queryParameters.SortOrder);

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResult<Floor>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize
            };
        }

        public async Task<Floor> CreateAsync(Floor floor)
        {
            _contextAccessor.TenantDbContext.Floors.Add(floor);
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return floor;
        }

        public async Task<Floor> UpdateAsync(Floor floor)
        {
            _contextAccessor.TenantDbContext.Entry(floor).State = EntityState.Modified;
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return floor;
        }

        public async Task DeleteAsync(int id)
        {
            var floor = await _contextAccessor.TenantDbContext.Floors.FindAsync(id);
            if (floor != null)
            {
                _contextAccessor.TenantDbContext.Floors.Remove(floor);
                await _contextAccessor.TenantDbContext.SaveChangesAsync();
            }
        }

        public async Task<PagedResult<Floor>> GetByBlockAsync(int blockId, QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Floors
                .Where(f => f.BlockId == blockId)
                .AsQueryable();

            query = query.ApplyFilters(queryParameters.Filters);

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(f => f.FloorNumber.ToString().Contains(queryParameters.SearchTerm));
            }

            query = query.ApplySort(queryParameters.SortBy, queryParameters.SortOrder);

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResult<Floor>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize
            };
        }
    }
}