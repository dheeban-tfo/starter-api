using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly IConfiguration _configuration;

    public AzureBlobStorageService(
        IConfiguration configuration, 
        ILogger<AzureBlobStorageService> logger)
    {
        _configuration = configuration;
        _blobServiceClient = new BlobServiceClient(configuration["AzureStorage:ConnectionString"]);
        _logger = logger;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string containerName, string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            await containerClient.CreateIfNotExistsAsync();
            await containerClient.SetAccessPolicyAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);
            var blobHttpHeaders = new BlobHttpHeaders
            {
                ContentType = file.ContentType
            };

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, blobHttpHeaders);

            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName} to container {ContainerName}", 
                fileName, containerName);
            throw new StorageException("Error uploading file", ex);
        }
    }

    public async Task<bool> DeleteFileAsync(string containerName, string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            
            return await blobClient.DeleteIfExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file {FileName} from container {ContainerName}", 
                fileName, containerName);
            throw new StorageException("Error deleting file", ex);
        }
    }

    public async Task<Stream> DownloadFileAsync(string containerName, string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            var download = await blobClient.DownloadAsync();
            return download.Value.Content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file {FileName} from container {ContainerName}", 
                fileName, containerName);
            throw new StorageException("Error downloading file", ex);
        }
    }

    public async Task<string> GetFileUrlAsync(string containerName, string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            
            return blobClient.Uri.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting URL for file {FileName} from container {ContainerName}", 
                fileName, containerName);
            throw new StorageException("Error getting file URL", ex);
        }
    }

    public async Task<bool> FileExistsAsync(string containerName, string fileName)
    {
        try
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
            var blobClient = containerClient.GetBlobClient(fileName);
            
            return await blobClient.ExistsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of file {FileName} in container {ContainerName}", 
                fileName, containerName);
            throw new StorageException("Error checking file existence", ex);
        }
    }
}