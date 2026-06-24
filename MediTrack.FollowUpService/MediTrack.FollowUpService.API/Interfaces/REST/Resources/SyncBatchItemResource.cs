namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record SyncBatchItemResource(
    int PatientId,
    string EntityType,
    string PayloadJson,
    DateTime QueuedAt
);
