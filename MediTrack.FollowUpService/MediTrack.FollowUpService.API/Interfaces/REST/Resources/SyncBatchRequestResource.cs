namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record SyncBatchRequestResource(List<SyncBatchItemResource> Items);
