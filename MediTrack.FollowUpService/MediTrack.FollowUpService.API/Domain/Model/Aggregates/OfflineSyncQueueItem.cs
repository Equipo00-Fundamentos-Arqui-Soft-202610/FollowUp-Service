namespace MediTrack.FollowUpService.API.Domain.Model.Aggregates;

public class OfflineSyncQueueItem
{
    public int Id { get; private set; }
    public int PatientId { get; private set; }
    public string EntityType { get; private set; } = string.Empty;
    public string Payload { get; private set; } = string.Empty;
    public DateTime QueuedAt { get; private set; }
    public DateTime? SyncedAt { get; private set; }
    public string Status { get; private set; } = "pending";

    protected OfflineSyncQueueItem() { }

    public OfflineSyncQueueItem(int patientId, string entityType, string payload, DateTime queuedAt)
    {
        PatientId = patientId;
        EntityType = entityType;
        Payload = payload;
        QueuedAt = queuedAt;
        Status = "pending";
    }

    public void MarkSynced()
    {
        Status = "synced";
        SyncedAt = DateTime.UtcNow;
    }

    public void MarkFailed()
    {
        Status = "failed";
    }
}
