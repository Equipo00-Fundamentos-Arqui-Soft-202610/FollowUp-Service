namespace MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;

/// <summary>
/// Mensaje saliente persistido en la misma transacción local que el cambio de
/// dominio (patrón Outbox). Un publicador en background lo entrega luego a
/// RabbitMQ, garantizando que ningún evento se pierda si el broker está caído
/// justo en el momento de publicar.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAtUtc { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
