using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public static class ProcessSyncBatchCommandFromResourceAssembler
{
    public static ProcessSyncBatchCommand ToCommandFromResource(SyncBatchRequestResource resource) =>
        new(resource.Items.Select(i => new SyncQueueItemDto(
            i.PatientId, i.EntityType, i.PayloadJson, i.QueuedAt)).ToList());
}
