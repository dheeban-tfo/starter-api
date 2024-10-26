using System.Globalization;
using System.IO;
using System.Security.Claims;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using starterapi;
using StarterApi.Helpers;
using starterapi.Models;
using StarterApi.Models;
using StarterApi.Repositories;
using System.ComponentModel.DataAnnotations;

namespace StarterApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Module(ModuleName.CommunityManagement)]
    public class CommunityController : ControllerBase
    {
        private readonly ICommunityRepository _communityRepository;
        private readonly ILogger<CommunityController> _logger;
        private readonly IFileService _fileService;

        public CommunityController(
            ICommunityRepository communityRepository,
            ILogger<CommunityController> logger,
            IFileService fileService
        )
        {
            _communityRepository = communityRepository;
            _logger = logger;
            _fileService = fileService;
        }

        // [NonAction]
        // private void SetCurrentUserId()
        // {
        //     var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //     if (!string.IsNullOrEmpty(userId))
        //     {
        //         UserContext.CurrentUserId = userId;
        //     }
        // }

        [HttpGet("{id}")]
        public async Task<ActionResult<CommunityDto>> GetCommunity(int id)
        {
            var community = await _communityRepository.GetByIdAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return new CommunityDto
            {
                Id = community.Id,
                Name = community.Name,
                Address = community.Address
            };
        }

        //  [HttpGet("all")]
        // public async Task<ActionResult<IEnumerable<Community>>> GetAllCommunities()
        // {
        //     return await _communityRepository.GetAllAsync();
        // }

        [HttpGet]
        public async Task<ActionResult<PagedResult<CommunityDto>>> GetAllCommunities(
            [FromQuery] QueryParameters queryParameters
        )
        {
            var result = await _communityRepository.GetAllAsync(queryParameters);
            var dtos = new PagedResult<CommunityDto>
            {
                Items = result
                    .Items.Select(c => new CommunityDto
                    {
                        Id = c.Id,
                        Name = c.Name,
                        Address = c.Address
                    })
                    .ToList(),
                TotalItems = result.TotalItems,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };
            return dtos;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Create))]
        public async Task<ActionResult<CommunityDto>> CreateCommunity(CommunityDto communityDto)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized("User ID not found in token");
            }

            var community = new Community
            {
                Name = communityDto.Name,
                Address = communityDto.Address,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId,
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = userId,
                IsActive = true,
                Version = DateTime.UtcNow.Ticks
            };

            var createdCommunity = await _communityRepository.CreateAsync(community);

            var createdCommunityDto = new CommunityDto
            {
                Id = createdCommunity.Id,
                Name = createdCommunity.Name,
                Address = createdCommunity.Address
            };

            return CreatedAtAction(
                nameof(GetCommunity),
                new { id = createdCommunityDto.Id },
                createdCommunityDto
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCommunity(int id, Community community)
        {
            if (id != community.Id)
            {
                return BadRequest();
            }

            community.ModifiedAt = DateTime.UtcNow;
            await _communityRepository.UpdateAsync(community);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCommunity(int id)
        {
            await _communityRepository.DeleteAsync(id);
            return NoContent();
        }

        [HttpGet("{id}/withBlocks")]
        public async Task<ActionResult<CommunityWithBlocksDto>> GetCommunityWithBlocks(int id)
        {
            var community = await _communityRepository.GetCommunityWithBlocksAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return community;
        }

        [HttpGet("{id}/withBlocksAndFloors")]
        public async Task<
            ActionResult<CommunityWithBlocksAndFloorsDto>
        > GetCommunityWithBlocksAndFloors(int id)
        {
            var community = await _communityRepository.GetCommunityWithBlocksAndFloorsAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return community;
        }

        [HttpGet("{id}/full")]
        public async Task<ActionResult<CommunityFullDto>> GetCommunityFull(int id)
        {
            var community = await _communityRepository.GetCommunityFullAsync(id);
            if (community == null)
            {
                return NotFound();
            }
            return community;
        }

        [HttpGet("statistics")]
        [Authorize(Policy = "PermissionPolicy")]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<CommunityStatisticsDto>> GetCommunityStatistics()
        {
            try
            {
                var statistics = await _communityRepository.GetCommunityStatisticsAsync();
                return Ok(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving community statistics");
                return StatusCode(500, "An error occurred while retrieving community statistics");
            }
        }

        [HttpGet("basic-stats")]
        public async Task<ActionResult<List<CommunityBasicStatsDto>>> GetAllCommunityBasicStats()
        {
            try
            {
                var basicStats = await _communityRepository.GetAllCommunityBasicStatsAsync();
                return Ok(basicStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving basic community statistics");
                return StatusCode(
                    500,
                    "An error occurred while retrieving basic community statistics"
                );
            }
        }

        [HttpPost("{communityId}/import")]
        [Authorize(Roles = "Admin,Super Admin")]
        public async Task<IActionResult> ImportCommunityData(int communityId, IFormFile file)
        {
            _logger.LogInformation($"Attempting to import data for community {communityId}");

           // SetCurrentUserId();

            if (file == null || file.Length == 0)
            {
                _logger.LogWarning("File is empty or null");
                return BadRequest("File is empty");
            }

            try
            {
                using (var reader = new StreamReader(file.OpenReadStream()))
                using (
                    var csv = new CsvReader(
                        reader,
                        new CsvConfiguration(CultureInfo.InvariantCulture)
                        {
                            HeaderValidated = null,
                            MissingFieldFound = null
                        }
                    )
                )
                {
                    var records = csv.GetRecords<CommunityImportDto>().ToList();
                    await _communityRepository.ImportCommunityDataAsync(communityId, records);
                }

                _logger.LogInformation($"Successfully imported data for community {communityId}");
                return Ok("Data imported successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error importing community data for community {communityId}");
                return StatusCode(
                    500,
                    $"An error occurred while importing community data: {ex.Message}"
                );
            }
        }

        [HttpPost("{id}/files")]
        [Authorize(Roles = "Admin,Super Admin")]
        [Permission(nameof(ModuleActions.CommunityManagement.Create))]
        public async Task<ActionResult<FileUploadResponse>> UploadCommunityFile(
            int id,
            IFormFile file,
            [FromQuery] FileType fileType = FileType.Image
        )
        {
            try
            {
                if (file == null)
        {
            return BadRequest(new { error = "No file was provided" });
        }
        
                // Verify community exists
                var community = await _communityRepository.GetByIdAsync(id);
                if (community == null)
                {
                    return NotFound($"Community with ID {id} not found");
                }

                var result = await _fileService.UploadFileAsync(file, "Community", id, fileType);
                return Ok(result);
            }
  catch (System.ComponentModel.DataAnnotations.ValidationException ex)  // Add full namespace
    {
        _logger.LogWarning("File validation failed: {Message}", ex.Message);
        return BadRequest(new { error = ex.Message });
    }
    catch (StorageException ex)
    {
        _logger.LogError(ex, "Storage error while uploading file for community {CommunityId}", id);
        return StatusCode(500, new { error = "Error storing the file. Please try again later." });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error uploading file for community {CommunityId}", id);
        return StatusCode(500, new { error = "An unexpected error occurred while uploading the file. Please try again later." });
    }
        }

        [HttpGet("{id}/files")]
        [Authorize]
        [Permission(nameof(ModuleActions.CommunityManagement.Read))]
        public async Task<ActionResult<IEnumerable<FileResponse>>> GetCommunityFiles(int id)
        {
            try
            {
                // Add debugging information
            _logger.LogInformation("User Claims: {Claims}", 
                string.Join(", ", User.Claims.Select(c => $"{c.Type}: {c.Value}")));


                // Verify community exists
                var community = await _communityRepository.GetByIdAsync(id);
                if (community == null)
                {
                    return NotFound($"Community with ID {id} not found");
                }

                var files = await _fileService.GetFilesAsync("Community", id);
                return Ok(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving files for community {CommunityId}", id);
                return StatusCode(500, "An error occurred while retrieving files");
            }
        }

        [HttpDelete("{id}/files/{fileId}")]
[Authorize]
[Permission(nameof(ModuleActions.CommunityManagement.Delete))]
        public async Task<IActionResult> DeleteCommunityFile(
            int id,
            int fileId,
            [FromQuery] bool hardDelete = false
        )
        {
            try
            {
                // Verify community exists
                var community = await _communityRepository.GetByIdAsync(id);
                if (community == null)
                {
                    return NotFound($"Community with ID {id} not found");
                }

                // Verify file belongs to this community
                var files = await _fileService.GetFilesAsync("Community", id);
                if (!files.Any(f => f.Id == fileId))
                {
                    return NotFound($"File with ID {fileId} not found for community {id}");
                }

                var result = await _fileService.DeleteFileAsync(fileId, hardDelete);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error deleting file {FileId} for community {CommunityId}",
                    fileId,
                    id
                );
                return StatusCode(500, "An error occurred while deleting the file");
            }
        }

        [HttpGet("{id}/files/{fileId}/download")]
[Authorize]
[Permission(nameof(ModuleActions.CommunityManagement.Read))]

        public async Task<IActionResult> DownloadCommunityFile(int id, int fileId)
        {
            try
            {
                // Verify community exists
                var community = await _communityRepository.GetByIdAsync(id);
                if (community == null)
                {
                    return NotFound($"Community with ID {id} not found");
                }

                // Verify file belongs to this community
                var files = await _fileService.GetFilesAsync("Community", id);
                if (!files.Any(f => f.Id == fileId))
                {
                    return NotFound($"File with ID {fileId} not found for community {id}");
                }

                var fileStream = await _fileService.DownloadFileAsync(fileId);
                var file = await _fileService.GetFileAsync(fileId);
                return File(fileStream, file.ContentType, file.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error downloading file {FileId} for community {CommunityId}",
                    fileId,
                    id
                );
                return StatusCode(500, "An error occurred while downloading the file");
            }
        }
    }
}
