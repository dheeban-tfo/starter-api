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

       public async Task<CommunityStatisticsDto> GetCommunityStatisticsAsync()
{
    var context = _contextAccessor.TenantDbContext;

    var totalCommunities = await context.Communities.CountAsync();
    var totalBlocks = await context.Blocks.CountAsync();
    var totalUnits = await context.Units.CountAsync();

    var communityStats = await context.Communities
        .Select(c => new
        {
            c.Id,
            c.Name,
            BlockCount = c.Blocks.Count(),
            UnitCount = c.Blocks.SelectMany(b => b.Floors).SelectMany(f => f.Units).Count()
        })
        .ToListAsync();

    var mostPopulousCommunity = communityStats
        .OrderByDescending(c => c.UnitCount)
        .FirstOrDefault();

    var communityWithMostBlocks = communityStats
        .OrderByDescending(c => c.BlockCount)
        .FirstOrDefault();

    return new CommunityStatisticsDto
    {
        TotalCommunities = totalCommunities,
        TotalBlocks = totalBlocks,
        TotalUnits = totalUnits,
        AverageBlocksPerCommunity = totalCommunities > 0 ? (double)totalBlocks / totalCommunities : 0,
        AverageUnitsPerCommunity = totalCommunities > 0 ? (double)totalUnits / totalCommunities : 0,
        MostPopulousCommunity = mostPopulousCommunity != null
            ? new CommunityBasicStatsDto
            {
                Id = mostPopulousCommunity.Id,
                Name = mostPopulousCommunity.Name,
                UnitCount = mostPopulousCommunity.UnitCount
            }
            : null,
        CommunityWithMostBlocks = communityWithMostBlocks != null
            ? new CommunityBasicStatsDto
            {
                Id = communityWithMostBlocks.Id,
                Name = communityWithMostBlocks.Name,
                BlockCount = communityWithMostBlocks.BlockCount
            }
            : null
    };
}

        public async Task<List<CommunityBasicStatsDto>> GetAllCommunityBasicStatsAsync()
{
    var context = _contextAccessor.TenantDbContext;

    return await context.Communities
        .Select(c => new CommunityBasicStatsDto
        {
            Id = c.Id,
            Name = c.Name,
            BlockCount = c.Blocks.Count(),
            UnitCount = c.Blocks.SelectMany(b => b.Floors).SelectMany(f => f.Units).Count()
        })
        .ToListAsync();
}
    }
}
