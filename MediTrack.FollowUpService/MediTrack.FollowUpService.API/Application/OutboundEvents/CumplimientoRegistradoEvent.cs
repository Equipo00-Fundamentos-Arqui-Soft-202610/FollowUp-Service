namespace MediTrack.FollowUpService.API.Application.OutboundEvents;

public record CumplimientoRegistradoEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public Guid CorrelationId { get; init; } = Guid.NewGuid();
    public string EventType => "CumplimientoRegistrado";
    public int PatientId { get; init; }
    public string EntityType { get; init; } = "Medication";
    public int EntityId { get; init; }
}
