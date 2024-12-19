using AutoMapper;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

using StarterApi.Repositories;
using starterapi.Models;
using StarterApi.Helpers;
using StarterApi.Models.Communities;

namespace Modules.Community.Services
{
    public class FloorService : IFloorService
    {
        private readonly IFloorRepository _floorRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<FloorService> _logger;

        public FloorService(
            IFloorRepository floorRepository,
            IMapper mapper,
            ILogger<FloorService> logger)
        {
            _floorRepository = floorRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<FloorDto> GetByIdAsync(int id)
        {
            var floor = await _floorRepository.GetByIdAsync(id);
            return _mapper.Map<FloorDto>(floor);
        }

        public async Task<PagedResult<FloorDto>> GetAllAsync(QueryParameters queryParameters)
        {
            var floors = await _floorRepository.GetAllAsync(queryParameters);
            return _mapper.Map<PagedResult<FloorDto>>(floors);
        }

        public async Task<FloorDto> CreateAsync(CreateFloorDto createFloorDto)
        {
            var floor = _mapper.Map<Floor>(createFloorDto);
            floor.CreatedAt = DateTime.UtcNow;
            floor.CreatedBy = UserContext.CurrentUserId;
            floor.IsActive = true;

            var createdFloor = await _floorRepository.CreateAsync(floor);
            return _mapper.Map<FloorDto>(createdFloor);
        }

        public async Task<FloorDto> UpdateAsync(int id, UpdateFloorDto updateFloorDto)
        {
            var existingFloor = await _floorRepository.GetByIdAsync(id);
            if (existingFloor == null)
            {
                throw new NotFoundException($"Floor with ID {id} not found");
            }

            _mapper.Map(updateFloorDto, existingFloor);
            existingFloor.ModifiedAt = DateTime.UtcNow;
            existingFloor.ModifiedBy = UserContext.CurrentUserId;

            var updatedFloor = await _floorRepository.UpdateAsync(existingFloor);
            return _mapper.Map<FloorDto>(updatedFloor);
        }

        public async Task DeleteAsync(int id)
        {
            await _floorRepository.DeleteAsync(id);
        }

        public async Task<PagedResult<FloorDto>> GetByBlockAsync(int blockId, QueryParameters queryParameters)
        {
            var floors = await _floorRepository.GetByBlockAsync(blockId, queryParameters);
            return _mapper.Map<PagedResult<FloorDto>>(floors);
        }
    }
} 