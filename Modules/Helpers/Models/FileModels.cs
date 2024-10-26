using StarterApi.Models;

public class FileUploadResponse
{
    public int Id { get; set; }
    public string FileName { get; set; }
    public string Url { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
}

public class FileResponse : FileUploadResponse
{
    public FileType FileType { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StorageException : Exception
{
    public StorageException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}

public enum FileType
{
    Image,
    Document,
    Video,
    Other
}

public class FileEntity : BaseEntity
{
    public string FileName { get; set; }
    public string ContentType { get; set; }
    public long Size { get; set; }
    public string BlobUrl { get; set; }
    public string ContainerName { get; set; }
    public string EntityType { get; set; }
    public int EntityId { get; set; }
    public FileType FileType { get; set; }
}

public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}