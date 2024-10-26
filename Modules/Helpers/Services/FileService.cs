using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StarterApi.Helpers;


public interface IFileService
{
    Task<FileUploadResponse> UploadFileAsync(IFormFile file, string entityType, int entityId, FileType fileType);
    Task<IEnumerable<FileResponse>> GetFilesAsync(string entityType, int entityId);
    Task<FileResponse> GetFileAsync(int fileId);
    Task<bool> DeleteFileAsync(int fileId, bool hardDelete = false);
    Task<Stream> DownloadFileAsync(int fileId);
}

public class FileService : IFileService
{
    private readonly IFileRepository _fileRepository;
    private readonly IStorageService _storageService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<FileService> _logger;

    public FileService(
        IFileRepository fileRepository,
        IStorageService storageService,
        IConfiguration configuration,
        ILogger<FileService> logger)
    {
        _fileRepository = fileRepository;
        _storageService = storageService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<FileUploadResponse> UploadFileAsync(
        IFormFile file, 
        string entityType, 
        int entityId, 
        FileType fileType)
    {
        ValidateFile(file, fileType);

        var fileName = GenerateFileName(file.FileName);
        var containerName = GetContainerName(entityType);

        var blobUrl = await _storageService.UploadFileAsync(file, containerName, fileName);

        var fileEntity = new FileEntity
        {
            FileName = fileName,
            ContentType = file.ContentType,
            Size = file.Length,
            BlobUrl = blobUrl,
            ContainerName = containerName,
            EntityType = entityType,
            EntityId = entityId,
            FileType = fileType,
            CreatedBy = UserContext.CurrentUserId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var savedFile = await _fileRepository.SaveFileInfoAsync(fileEntity);

        return new FileUploadResponse
        {
            Id = savedFile.Id,
            FileName = savedFile.FileName,
            Url = savedFile.BlobUrl,
            ContentType = savedFile.ContentType,
            Size = savedFile.Size
        };
    }

    public async Task<IEnumerable<FileResponse>> GetFilesAsync(string entityType, int entityId)
    {
        var files = await _fileRepository.GetFilesByEntityAsync(entityType, entityId);
        return files.Select(f => new FileResponse
        {
            Id = f.Id,
            FileName = f.FileName,
            Url = f.BlobUrl,
            ContentType = f.ContentType,
            Size = f.Size,
            FileType = f.FileType,
            CreatedAt = f.CreatedAt
        });
    }

    public async Task<FileResponse> GetFileAsync(int fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null) return null;

        return new FileResponse
        {
            Id = file.Id,
            FileName = file.FileName,
            Url = file.BlobUrl,
            ContentType = file.ContentType,
            Size = file.Size,
            FileType = file.FileType,
            CreatedAt = file.CreatedAt
        };
    }

    public async Task<bool> DeleteFileAsync(int fileId, bool hardDelete = false)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null) return false;

        if (hardDelete)
        {
            await _storageService.DeleteFileAsync(file.ContainerName, file.FileName);
        }

        return await _fileRepository.DeleteFileAsync(fileId, hardDelete);
    }

    public async Task<Stream> DownloadFileAsync(int fileId)
    {
        var file = await _fileRepository.GetByIdAsync(fileId);
        if (file == null) throw new NotFoundException($"File with ID {fileId} not found");

        return await _storageService.DownloadFileAsync(file.ContainerName, file.FileName);
    }

    private void ValidateFile(IFormFile file, FileType fileType)
    {
        if (file == null || file.Length == 0)
            throw new ValidationException("File is empty or invalid");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        
        var allowedExtensions = GetAllowedExtensions(fileType);
        if (!allowedExtensions.Contains(extension))
        {
            throw new ValidationException(
                $"File type {extension} is not allowed. Allowed types for {fileType} are: {string.Join(", ", allowedExtensions)}"
            );
        }

        // Add file size validation
        var maxSize = GetMaxFileSize(fileType);
        if (file.Length > maxSize)
        {
            throw new ValidationException(
                $"File size exceeds maximum limit of {maxSize / (1024 * 1024)}MB for {fileType}"
            );
        }
    }

    private string[] GetAllowedExtensions(FileType fileType)
    {
        return fileType switch
        {
            FileType.Document => new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx" },
            FileType.Image => new[] { ".jpg", ".jpeg", ".png", ".gif" },
            FileType.Video => new[] { ".mp4", ".avi", ".mov" },
            _ => throw new ValidationException($"Unsupported file type: {fileType}")
        };
    }

    private long GetMaxFileSize(FileType fileType)
    {
        return fileType switch
        {
            FileType.Document => 10 * 1024 * 1024, // 10MB
            FileType.Image => 5 * 1024 * 1024,     // 5MB
            FileType.Video => 100 * 1024 * 1024,   // 100MB
            _ => throw new ValidationException($"Unsupported file type: {fileType}")
        };
    }

    private string GenerateFileName(string originalFileName)
    {
        var extension = Path.GetExtension(originalFileName);
        return $"{Guid.NewGuid()}{extension}";
    }

    private string GetContainerName(string entityType)
    {
        return _configuration[$"AzureStorage:Containers:{entityType}"] 
            ?? throw new ValidationException($"Container not configured for entity type {entityType}");
    }
}
