using Microsoft.EntityFrameworkCore;
using starterapi.Models;
using starterapi.Services;
using StarterApi.Models;
using starterapi.Extensions;
using starterapi.Repositories;

namespace StarterApi.Repositories
{
    public class UnitRepository : IUnitRepository
    {
        private readonly ITenantDbContextAccessor _contextAccessor;

        public UnitRepository(ITenantDbContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task<Unit> GetByIdAsync(int id)
        {
            return await _contextAccessor.TenantDbContext.Units
                .Include(u => u.UnitOwnerships)
                .Include(u => u.UnitResidents)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<PagedResult<Unit>> GetAllAsync(QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Units
                .Include(u => u.Floor)
                .AsQueryable();

            query = query.ApplyFilters(queryParameters.Filters);

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(u => u.UnitNumber.Contains(queryParameters.SearchTerm));
            }

            query = query.ApplySort(queryParameters.SortBy, queryParameters.SortOrder);

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResult<Unit>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize
            };
        }

        public async Task<Unit> CreateAsync(Unit unit)
        {
            _contextAccessor.TenantDbContext.Units.Add(unit);
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return unit;
        }

        public async Task<Unit> UpdateAsync(Unit unit)
        {
            _contextAccessor.TenantDbContext.Entry(unit).State = EntityState.Modified;
            await _contextAccessor.TenantDbContext.SaveChangesAsync();
            return unit;
        }

        public async Task DeleteAsync(int id)
        {
            var unit = await _contextAccessor.TenantDbContext.Units.FindAsync(id);
            if (unit != null)
            {
                _contextAccessor.TenantDbContext.Units.Remove(unit);
                await _contextAccessor.TenantDbContext.SaveChangesAsync();
            }
        }

        public async Task<PagedResult<Unit>> GetByFloorAsync(int floorId, QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Units
                .Where(u => u.FloorId == floorId)
                .AsQueryable();

            query = query.ApplyFilters(queryParameters.Filters);

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(u => u.UnitNumber.Contains(queryParameters.SearchTerm));
            }

            query = query.ApplySort(queryParameters.SortBy, queryParameters.SortOrder);

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .ToListAsync();

            return new PagedResult<Unit>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize
            };
        }
    }
}