namespace MediTrack.FollowUpService.API.Domain.Model.Commands;

public record SyncQueueItemDto(
    int PatientId,
    string EntityType,
    string PayloadJson,
    DateTime QueuedAt
);

public record ProcessSyncBatchCommand(List<SyncQueueItemDto> Items);
