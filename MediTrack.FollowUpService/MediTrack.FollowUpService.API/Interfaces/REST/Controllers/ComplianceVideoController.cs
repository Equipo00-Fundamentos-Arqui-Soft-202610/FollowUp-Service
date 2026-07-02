using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Controllers;

[ApiController]
[Route("api/v1/compliance/video")]
public class ComplianceVideoController : ControllerBase
{
    private readonly IBlobStorageService _blobStorageService;
    private readonly ILogger<ComplianceVideoController> _logger;

    public ComplianceVideoController(IBlobStorageService blobStorageService,
        ILogger<ComplianceVideoController> logger)
    {
        _blobStorageService = blobStorageService;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(VideoUploadResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<VideoUploadResource>> UploadVideo(
        IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file provided" });

        var allowedTypes = new[] { "video/mp4", "video/quicktime", "video/x-msvideo" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { message = "Only MP4, MOV and AVI videos are accepted" });

        const long maxSizeBytes = 100 * 1024 * 1024;
        if (file.Length > maxSizeBytes)
            return BadRequest(new { message = "File exceeds maximum size of 100 MB" });

        try
        {
            await using var stream = file.OpenReadStream();
            var url = await _blobStorageService.UploadAsync(
                stream, file.FileName, file.ContentType);

            return Ok(new VideoUploadResource(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video to Blob Storage");
            return StatusCode(500, new { message = "Error uploading video, please try again" });
        }
    }
}
