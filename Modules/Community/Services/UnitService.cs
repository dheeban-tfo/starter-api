using AutoMapper;
using starterapi.Models;
using StarterApi.Helpers;
using StarterApi.Models.Communities;
using StarterApi.Repositories;
using System;
using System.Threading.Tasks;

public class UnitService : IUnitService
{
    private readonly IUnitRepository _unitRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<UnitService> _logger;

    public UnitService(
        IUnitRepository unitRepository,
        IMapper mapper,
        ILogger<UnitService> logger)
    {
        _unitRepository = unitRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<UnitDto> GetByIdAsync(int id)
    {
        var unit = await _unitRepository.GetByIdAsync(id);
        return _mapper.Map<UnitDto>(unit);
    }

    public async Task<PagedResult<UnitDto>> GetAllAsync(QueryParameters queryParameters)
    {
        var units = await _unitRepository.GetAllAsync(queryParameters);
        return _mapper.Map<PagedResult<UnitDto>>(units);
    }

    public async Task<UnitDto> CreateAsync(CreateUnitDto createUnitDto)
    {
        var unit = _mapper.Map<Unit>(createUnitDto);
        unit.CreatedAt = DateTime.UtcNow;
        unit.CreatedBy = UserContext.CurrentUserId;
        unit.IsActive = true;

        var createdUnit = await _unitRepository.CreateAsync(unit);
        return _mapper.Map<UnitDto>(createdUnit);
    }

    public async Task<UnitDto> UpdateAsync(int id, UpdateUnitDto updateUnitDto)
    {
        var existingUnit = await _unitRepository.GetByIdAsync(id);
        if (existingUnit == null)
        {
            throw new NotFoundException($"Unit with ID {id} not found");
        }

        _mapper.Map(updateUnitDto, existingUnit);
        existingUnit.ModifiedAt = DateTime.UtcNow;
        existingUnit.ModifiedBy = UserContext.CurrentUserId;

        var updatedUnit = await _unitRepository.UpdateAsync(existingUnit);
        return _mapper.Map<UnitDto>(updatedUnit);
    }

    public async Task DeleteAsync(int id)
    {
        await _unitRepository.DeleteAsync(id);
    }

    public async Task<PagedResult<UnitDto>> GetByFloorAsync(int floorId, QueryParameters queryParameters)
    {
        var units = await _unitRepository.GetByFloorAsync(floorId, queryParameters);
        return _mapper.Map<PagedResult<UnitDto>>(units);
    }
} 