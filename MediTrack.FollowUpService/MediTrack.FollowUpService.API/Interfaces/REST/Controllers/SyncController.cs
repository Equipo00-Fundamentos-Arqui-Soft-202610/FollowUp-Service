using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;
using MediTrack.FollowUpService.API.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/sync")]
public class SyncController : ControllerBase
{
    private readonly IOfflineSyncCommandService _syncService;

    public SyncController(IOfflineSyncCommandService syncService)
    {
        _syncService = syncService;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> ProcessBatch([FromBody] SyncBatchRequestResource resource)
    {
        try
        {
            if (resource.Items is null || resource.Items.Count == 0)
                return BadRequest(new { message = "Items list cannot be empty" });
            if (resource.Items.Count > 100)
                return BadRequest(new { message = "Batch size cannot exceed 100 items" });

            var command = ProcessSyncBatchCommandFromResourceAssembler.ToCommandFromResource(resource);
            var results = await _syncService.Handle(command);

            var resultResources = results.Select(r => new SyncBatchResultResource(
                r.EntityType, r.CreatedId, r.Status, r.ErrorMessage)).ToList();

            return Ok(new
            {
                totalProcessed = resultResources.Count,
                syncedCount = resultResources.Count(r => r.Status == "synced"),
                failedCount = resultResources.Count(r => r.Status == "failed"),
                results = resultResources
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
