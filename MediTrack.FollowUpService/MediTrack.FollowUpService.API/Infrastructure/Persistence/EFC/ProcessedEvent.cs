namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;

/// <summary>
/// Registro de eventos ya consumidos (patrón Inbox). Garantiza el procesamiento
/// idempotente: si RabbitMQ reentrega un mensaje, no se vuelve a aplicar.
/// </summary>
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public DateTime ProcessedAtUtc { get; set; }
}
