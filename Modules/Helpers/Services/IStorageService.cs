public interface IStorageService
{
    Task<string> UploadFileAsync(IFormFile file, string containerName, string fileName);
    Task<bool> DeleteFileAsync(string containerName, string fileName);
    Task<Stream> DownloadFileAsync(string containerName, string fileName);
    Task<string> GetFileUrlAsync(string containerName, string fileName);
    Task<bool> FileExistsAsync(string containerName, string fileName);
}