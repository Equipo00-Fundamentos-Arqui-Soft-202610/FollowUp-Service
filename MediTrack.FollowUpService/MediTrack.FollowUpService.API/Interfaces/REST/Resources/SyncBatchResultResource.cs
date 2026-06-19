namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record SyncBatchResultResource(
    string EntityType,
    int? CreatedId,
    string Status,
    string? ErrorMessage
);
