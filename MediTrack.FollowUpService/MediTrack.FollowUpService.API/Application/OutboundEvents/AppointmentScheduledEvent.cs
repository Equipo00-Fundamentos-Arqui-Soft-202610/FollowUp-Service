namespace MediTrack.FollowUpService.API.Application.OutboundEvents;

public record AppointmentScheduledEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public int PatientId { get; init; }
    public int AppointmentId { get; init; }
    public string AppointmentType { get; init; } = string.Empty;
    public string? Location { get; init; }
    public DateTime AppointmentDateUtc { get; init; }
}
