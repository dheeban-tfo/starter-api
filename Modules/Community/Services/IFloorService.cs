using starterapi.Models;

public interface IFloorService
{
    Task<FloorDto> GetByIdAsync(int id);
    Task<PagedResult<FloorDto>> GetAllAsync(QueryParameters queryParameters);
    Task<FloorDto> CreateAsync(CreateFloorDto createFloorDto);
    Task<FloorDto> UpdateAsync(int id, UpdateFloorDto updateFloorDto);
    Task DeleteAsync(int id);
    Task<PagedResult<FloorDto>> GetByBlockAsync(int blockId, QueryParameters queryParameters);
} 