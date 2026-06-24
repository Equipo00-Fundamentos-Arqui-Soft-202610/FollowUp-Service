namespace MediTrack.FollowUpService.API.Application.OutboundEvents;

public record ComplianceRegisteredEvent
{
    public int PatientId { get; init; }
    public string Category { get; init; } = "medication";
    public bool WasCompliant { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
