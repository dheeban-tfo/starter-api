using Microsoft.EntityFrameworkCore;
using starterapi;
using starterapi.Extensions;
using starterapi.Models;
using StarterApi.Models;
using starterapi.Repositories;
using starterapi.Services;

namespace StarterApi.Repositories
{
    public class CommunityRepository : ICommunityRepository
    {
        private readonly ITenantDbContextAccessor _contextAccessor;

        public CommunityRepository(ITenantDbContextAccessor contextAccessor)
        {
            _contextAccessor = contextAccessor;
        }

        public async Task<CommunityDto> GetByIdAsync(int id)
        {
            var community = await _contextAccessor.TenantDbContext.Communities.FirstOrDefaultAsync(
                c => c.Id == id
            );

            if (community == null)
            {
                return null;
            }

            return new CommunityDto
            {
                Id = community.Id,
                Name = community.Name,
                Address = community.Address
                // Map other properties as needed
            };
        }

        async Task<IEnumerable<Community>> GetAllAsync()
        {
            return await _contextAccessor
                .TenantDbContext.Communities.Include(c => c.Blocks)
                .ThenInclude(b => b.Floors)
                .ThenInclude(f => f.Units)
                .ToListAsync();
        }

        public async Task<PagedResult<CommunityDto>> GetAllAsync(QueryParameters queryParameters)
        {
            var query = _contextAccessor.TenantDbContext.Communities.AsQueryable();

            // Apply filtering
            if (!string.IsNullOrEmpty(queryParameters.SearchTerm))
            {
                query = query.Where(c =>
                    c.Name.Contains(queryParameters.SearchTerm)
                    || c.Address.Contains(queryParameters.SearchTerm)
                );
            }

            // Apply sorting
            if (!string.IsNullOrEmpty(queryParameters.SortBy))
            {
                query = query.ApplySort(queryParameters.SortBy, queryParameters.SortOrder);
            }
            else
            {
                query = query.OrderBy(c => c.Name);
            }

            var totalItems = await query.CountAsync();

            var items = await query
                .Skip((queryParameters.PageNumber - 1) * queryParameters.PageSize)
                .Take(queryParameters.PageSize)
                .Select(c => new CommunityDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address
                })
                .ToListAsync();

            return new PagedResult<CommunityDto>
            {
                Items = items,
                TotalItems = totalItems,
                PageNumber = queryParameters.PageNumber,
                PageSize = queryParameters.PageSize
            };
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

        public async Task<CommunityWithBlocksDto> GetCommunityWithBlocksAsync(int id)
        {
            return await _contextAccessor
                .TenantDbContext.Communities.Where(c => c.Id == id)
                .Select(c => new CommunityWithBlocksDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Blocks = c
                        .Blocks.Select(b => new BlockDto { Id = b.Id, Name = b.Name })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CommunityWithBlocksAndFloorsDto> GetCommunityWithBlocksAndFloorsAsync(
            int id
        )
        {
            return await _contextAccessor
                .TenantDbContext.Communities.Where(c => c.Id == id)
                .Select(c => new CommunityWithBlocksAndFloorsDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Blocks = c
                        .Blocks.Select(b => new BlockWithFloorsDto
                        {
                            Id = b.Id,
                            Name = b.Name,
                            Floors = b
                                .Floors.Select(f => new FloorDto
                                {
                                    Id = f.Id,
                                    FloorNumber = f.FloorNumber
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }

        public async Task<CommunityFullDto> GetCommunityFullAsync(int id)
        {
            return await _contextAccessor
                .TenantDbContext.Communities.Where(c => c.Id == id)
                .Select(c => new CommunityFullDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Address = c.Address,
                    Blocks = c
                        .Blocks.Select(b => new BlockFullDto
                        {
                            Id = b.Id,
                            Name = b.Name,
                            Floors = b
                                .Floors.Select(f => new FloorFullDto
                                {
                                    Id = f.Id,
                                    FloorNumber = f.FloorNumber,
                                    Units = f
                                        .Units.Select(u => new UnitDto
                                        {
                                            Id = u.Id,
                                            UnitNumber = u.UnitNumber,
                                            Type = u.Type
                                        })
                                        .ToList()
                                })
                                .ToList()
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();
        }
    }
}
