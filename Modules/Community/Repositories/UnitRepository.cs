using Microsoft.EntityFrameworkCore;
using starterapi.Models;
using starterapi.Services;
using StarterApi.Models;
using starterapi.Extensions;
using starterapi.Repositories;
using StarterApi.Models.Communities;

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
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<Unit> GetByIdWithDetailsAsync(int id)
        {
            return await _contextAccessor.TenantDbContext.Units
                .Include(u => u.Floor)
                    .ThenInclude(f => f.Block)
                        .ThenInclude(b => b.Community)
               .Include(u => u.UnitOwnerships)
                    .ThenInclude(uo => uo.User.FirstName)
                .Include(u => u.UnitResidents)
                    .ThenInclude(ur => ur.User.FirstName)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<PagedResult<Unit>> GetAllAsync(QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Units
                .Include(u => u.Floor)
                .AsQueryable();

            return await ApplyQueryParameters(query, queryParameters);
        }

        public async Task<PagedResult<Unit>> GetAllWithDetailsAsync(QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Units
                .Include(u => u.Floor)
                    .ThenInclude(f => f.Block)
                        .ThenInclude(b => b.Community)
                .Include(u => u.UnitOwnerships)
                    .ThenInclude(uo => uo.User.FirstName)
                .Include(u => u.UnitResidents)
                    .ThenInclude(ur => ur.User.FirstName)
                .AsQueryable();

            return await ApplyQueryParameters(query, queryParameters);
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
                .Include(u => u.Floor)
                    .ThenInclude(f => f.Block)
                .AsQueryable();

            return await ApplyQueryParameters(query, queryParameters);
        }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _contextAccessor.TenantDbContext.Units
                .AnyAsync(u => u.Id == id);
        }

        public async Task<bool> IsUnitNumberUniqueAsync(string unitNumber, int floorId, int? excludeUnitId = null)
        {
            var query = _contextAccessor.TenantDbContext.Units
                .Where(u => u.UnitNumber == unitNumber && u.FloorId == floorId);

            if (excludeUnitId.HasValue)
            {
                query = query.Where(u => u.Id != excludeUnitId.Value);
            }

            return !await query.AnyAsync();
        }

        private async Task<PagedResult<Unit>> ApplyQueryParameters(IQueryable<Unit> query, QueryParameters queryParameters)
        {
            query = query.ApplyFilters(queryParameters.Filters);

            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(u => 
                    u.UnitNumber.Contains(queryParameters.SearchTerm) ||
                    u.Floor.Block.Name.Contains(queryParameters.SearchTerm) ||
                    u.Floor.FloorNumber.ToString().Contains(queryParameters.SearchTerm)
                );
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