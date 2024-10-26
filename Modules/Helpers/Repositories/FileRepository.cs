using Microsoft.EntityFrameworkCore;
using starterapi.Services;

public class FileRepository : IFileRepository
{
    private readonly ITenantDbContextAccessor _contextAccessor;
    private readonly IStorageService _storageService;

    public FileRepository(
        ITenantDbContextAccessor contextAccessor, 
        IStorageService storageService)
    {
        _contextAccessor = contextAccessor;
        _storageService = storageService;
    }

    public async Task<FileEntity> SaveFileInfoAsync(FileEntity fileEntity)
    {
        _contextAccessor.TenantDbContext.Files.Add(fileEntity);
        await _contextAccessor.TenantDbContext.SaveChangesAsync();
        return fileEntity;
    }

    public async Task<FileEntity> GetByIdAsync(int id)
    {
        return await _contextAccessor.TenantDbContext.Files
            .FirstOrDefaultAsync(f => f.Id == id && f.IsActive);
    }

    public async Task<IEnumerable<FileEntity>> GetFilesByEntityAsync(string entityType, int entityId)
    {
        return await _contextAccessor.TenantDbContext.Files
            .Where(f => f.EntityType == entityType && 
                       f.EntityId == entityId && 
                       f.IsActive)
            .ToListAsync();
    }

    public async Task<bool> DeleteFileAsync(int fileId, bool hardDelete = false)
    {
        var file = await GetByIdAsync(fileId);
        if (file == null) return false;

        if (hardDelete)
        {
            _contextAccessor.TenantDbContext.Files.Remove(file);
        }
        else
        {
            file.IsActive = false;
            _contextAccessor.TenantDbContext.Files.Update(file);
        }

        await _contextAccessor.TenantDbContext.SaveChangesAsync();
        return true;
    }
}