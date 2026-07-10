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

    /// <summary>Instante UTC de la toma específica que fue cumplida — Reminder-Service
    /// lo usa para saber cuál recordatorio (de qué día) cancelar.</summary>
    public DateTime OccurrenceDateUtc { get; init; }
}
