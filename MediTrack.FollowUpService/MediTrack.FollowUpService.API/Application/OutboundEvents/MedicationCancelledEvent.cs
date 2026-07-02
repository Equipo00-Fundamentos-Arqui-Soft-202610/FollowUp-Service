namespace MediTrack.FollowUpService.API.Application.OutboundEvents;

public record MedicationCancelledEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public int MedicationId { get; init; }
    public int PatientId { get; init; }
}
