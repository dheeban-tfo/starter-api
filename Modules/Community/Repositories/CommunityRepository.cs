using Microsoft.EntityFrameworkCore;
using starterapi;
using starterapi.Extensions;
using starterapi.Models;
using StarterApi.Models;
using starterapi.Repositories;
using starterapi.Services;
using StarterApi.Helpers;
using Microsoft.AspNetCore.Identity;

namespace StarterApi.Repositories
{
    public class CommunityRepository : ICommunityRepository
    {
        private readonly ITenantDbContextAccessor _contextAccessor;
        private readonly IPasswordHasher<User> _passwordHasher;
        private readonly ILogger<CommunityRepository> _logger;

        public CommunityRepository(ITenantDbContextAccessor contextAccessor, IPasswordHasher<User> passwordHasher, ILogger<CommunityRepository> logger)
        {
            _contextAccessor = contextAccessor;
            _passwordHasher = passwordHasher;
            _logger = logger;
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
            try
            {
                var existingCommunity = await _contextAccessor.TenantDbContext.Communities
                    .FirstOrDefaultAsync(c => c.Id == community.Id);

                if (existingCommunity == null)
                {
                    return null;
                }

                // Update only the specific properties
                existingCommunity.Name = community.Name;
                existingCommunity.Address = community.Address;
                existingCommunity.ModifiedAt = community.ModifiedAt;
                existingCommunity.ModifiedBy = community.ModifiedBy;
                existingCommunity.Version = community.Version;

                await _contextAccessor.TenantDbContext.SaveChangesAsync();
                return existingCommunity;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating community {Id}", community.Id);
                throw;
            }
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

            var communityStats = await context
                .Communities.Select(c => new
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
                AverageBlocksPerCommunity =
                    totalCommunities > 0 ? (double)totalBlocks / totalCommunities : 0,
                AverageUnitsPerCommunity =
                    totalCommunities > 0 ? (double)totalUnits / totalCommunities : 0,
                MostPopulousCommunity =
                    mostPopulousCommunity != null
                        ? new CommunityBasicStatsDto
                        {
                            Id = mostPopulousCommunity.Id,
                            Name = mostPopulousCommunity.Name,
                            UnitCount = mostPopulousCommunity.UnitCount
                        }
                        : null,
                CommunityWithMostBlocks =
                    communityWithMostBlocks != null
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

            return await context
                .Communities.Select(c => new CommunityBasicStatsDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    BlockCount = c.Blocks.Count(),
                    UnitCount = c.Blocks.SelectMany(b => b.Floors).SelectMany(f => f.Units).Count()
                })
                .ToListAsync();
        }

        public async Task ImportCommunityDataAsync(
            int communityId,
            List<CommunityImportDto> importData
        )
        {
            var community = await _contextAccessor.TenantDbContext.Communities
                .Include(c => c.Blocks)
                    .ThenInclude(b => b.Floors)
                        .ThenInclude(f => f.Units)
                            .ThenInclude(u => u.UnitOwnerships)
                .FirstOrDefaultAsync(c => c.Id == communityId);

            if (community == null)
            {
                throw new ArgumentException("Community not found", nameof(communityId));
            }

            var userId = UserContext.CurrentUserId;
            if (string.IsNullOrEmpty(userId))
            {
                throw new InvalidOperationException("User ID not found in the current context");
            }

            if (importData == null || !importData.Any())
            {
                throw new ArgumentException("Import data is empty or null", nameof(importData));
            }

            foreach (var blockGroup in importData.GroupBy(x => x.BlockName))
            {
                var block = community.Blocks.FirstOrDefault(b => b.Name == blockGroup.Key);
                if (block == null)
                {
                    block = new Block 
                    { 
                        Name = blockGroup.Key, 
                        Floors = new List<Floor>(),
                        CreatedBy = userId,
                        ModifiedBy = userId,
                        CreatedAt = DateTime.UtcNow,
                        ModifiedAt = DateTime.UtcNow
                    };
                    community.Blocks.Add(block);
                }

                foreach (var floorGroup in blockGroup.GroupBy(x => x.FloorNumber))
                {
                    var floor = block.Floors.FirstOrDefault(f => f.FloorNumber == floorGroup.Key);
                    if (floor == null)
                    {
                        floor = new Floor
                        {
                            FloorNumber = floorGroup.Key,
                            Units = new List<Unit>(),
                            CreatedBy = userId,
                            ModifiedBy = userId,
                            CreatedAt = DateTime.UtcNow,
                            ModifiedAt = DateTime.UtcNow
                        };
                        block.Floors.Add(floor);
                    }

                    foreach (var unitData in floorGroup)
                    {
                        var unit = floor.Units.FirstOrDefault(u => u.UnitNumber == unitData.UnitNumber);
                        if (unit == null)
                        {
                            unit = new Unit
                            {
                                UnitNumber = unitData.UnitNumber,
                                Type = unitData.UnitType,
                                UnitOwnerships = new List<UnitOwnership>(),
                                CreatedBy = userId,
                                ModifiedBy = userId,
                                CreatedAt = DateTime.UtcNow,
                                ModifiedAt = DateTime.UtcNow
                            };
                            floor.Units.Add(unit);
                        }

                        // Find or create user
                        var user = await FindOrCreateUserAsync(
                            unitData.OwnerName,
                            unitData.ContactNumber,
                            unitData.OwnerEmail
                        );

                        // Create or update UnitOwnership
                        var ownership = unit.UnitOwnerships.FirstOrDefault(uo => uo.UserId == user.Id);
                        if (ownership == null)
                        {
                            ownership = new UnitOwnership
                            {
                                UserId = user.Id,
                                OwnershipStartDate = unitData.OwnershipStartDate,
                                OwnershipEndDate = unitData.OwnershipEndDate,
                                OwnershipPercentage = unitData.OwnershipPercentage,
                                CreatedBy = userId,
                                ModifiedBy = userId,
                                CreatedAt = DateTime.UtcNow,
                                ModifiedAt = DateTime.UtcNow
                            };
                            unit.UnitOwnerships.Add(ownership);
                        }
                        else
                        {
                            ownership.OwnershipStartDate = unitData.OwnershipStartDate;
                            ownership.OwnershipEndDate = unitData.OwnershipEndDate;
                            ownership.OwnershipPercentage = unitData.OwnershipPercentage;
                            ownership.ModifiedBy = userId;
                            ownership.ModifiedAt = DateTime.UtcNow;
                        }
                    }
                }
            }

            await _contextAccessor.TenantDbContext.SaveChangesAsync();
        }

        private async Task<User> FindOrCreateUserAsync(
            string name,
            string contactNumber,
            string email
        )
        {
            var user = await _contextAccessor.TenantDbContext.Users.FirstOrDefaultAsync(u =>
                u.PhoneNumber == contactNumber || u.Email == email
            );

            if (user == null)
            {
                var userIdString = UserContext.CurrentUserId;
                if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out Guid userId))
                {
                    throw new InvalidOperationException("Invalid User ID in the current context");
                }

                user = new User
                {
                    Id = Guid.NewGuid(),
                    Email = email ?? throw new ArgumentNullException(nameof(email)),
                    PhoneNumber = contactNumber ?? throw new ArgumentNullException(nameof(contactNumber)),
                    FirstName = name ?? throw new ArgumentNullException(nameof(name)),
                    LastName = "",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    PasswordHash = _passwordHasher.HashPassword(null, "DefaultPassword123!"),
                    EmailVerified = false
                };

                _contextAccessor.TenantDbContext.Users.Add(user);
                await _contextAccessor.TenantDbContext.SaveChangesAsync();
            }

            return user;
        }
    }
}
