using starterapi.Models;

public interface IUnitService
{
    Task<UnitDto> GetByIdAsync(int id);
    Task<PagedResult<UnitDto>> GetAllAsync(QueryParameters queryParameters);
    Task<UnitDto> CreateAsync(CreateUnitDto createUnitDto);
    Task<UnitDto> UpdateAsync(int id, UpdateUnitDto updateUnitDto);
    Task DeleteAsync(int id);
    Task<PagedResult<UnitDto>> GetByFloorAsync(int floorId, QueryParameters queryParameters);
} 