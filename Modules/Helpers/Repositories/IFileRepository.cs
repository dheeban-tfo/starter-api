public interface IFileRepository
{
    Task<FileEntity> SaveFileInfoAsync(FileEntity fileEntity);
    Task<FileEntity> GetByIdAsync(int id);
    Task<IEnumerable<FileEntity>> GetFilesByEntityAsync(string entityType, int entityId);
    Task<bool> DeleteFileAsync(int fileId, bool hardDelete = false);
}
