using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using starterapi;
using starterapi.Models;
using StarterApi.Models;
using StarterApi.Repositories;
using System.IO;
using CsvHelper;
using System.Globalization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using CsvHelper.Configuration;
using StarterApi.Helpers;


[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FileController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly ILogger<FileController> _logger;

    public FileController(
        IFileService fileService,
        ILogger<FileController> logger)
    {
        _fileService = fileService;
        _logger = logger;
    }

    [HttpPost("{entityType}/{entityId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<FileUploadResponse>> UploadFile(
        string entityType,
        int entityId,
        IFormFile file,
        [FromQuery] FileType fileType = FileType.Other)
    {
        try
        {
            var result = await _fileService.UploadFileAsync(file, entityType, entityId, fileType);
            return Ok(result);
        }
        catch (ValidationException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file");
            return StatusCode(500, "An error occurred while uploading the file");
        }
    }

    [HttpGet("{entityType}/{entityId}")]
    public async Task<ActionResult<IEnumerable<FileResponse>>> GetFiles(
        string entityType,
        int entityId)
    {
        try
        {
            var files = await _fileService.GetFilesAsync(entityType, entityId);
            return Ok(files);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving files");
            return StatusCode(500, "An error occurred while retrieving files");
        }
    }

    [HttpDelete("{fileId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteFile(
        int fileId,
        [FromQuery] bool hardDelete = false)
    {
        try
        {
            var result = await _fileService.DeleteFileAsync(fileId, hardDelete);
            if (!result)
            {
                return NotFound();
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting file");
            return StatusCode(500, "An error occurred while deleting the file");
        }
    }

    [HttpGet("download/{fileId}")]
    public async Task<IActionResult> DownloadFile(int fileId)
    {
        try
        {
            var fileStream = await _fileService.DownloadFileAsync(fileId);
            var file = await _fileService.GetFileAsync(fileId);
            return File(fileStream, file.ContentType, file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading file");
            return StatusCode(500, "An error occurred while downloading the file");
        }
    }
}