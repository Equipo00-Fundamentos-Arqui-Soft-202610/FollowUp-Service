namespace MediTrack.FollowUpService.API.Application.OutboundEvents;

public record AppointmentComplianceRegisteredEvent
{
    public int PatientId { get; init; }
    public string Category { get; init; } = "appointment";
    public bool WasCompliant { get; init; }
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
