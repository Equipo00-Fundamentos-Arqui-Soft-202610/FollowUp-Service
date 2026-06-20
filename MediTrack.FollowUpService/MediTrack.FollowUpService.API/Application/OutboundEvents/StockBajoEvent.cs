namespace MediTrack.FollowUpService.API.Application.OutboundEvents;

public record StockBajoEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAtUtc { get; init; } = DateTime.UtcNow;
    public int PatientId { get; init; }
    public int MedicationId { get; init; }
    public string MedicationName { get; init; } = string.Empty;
    public int RemainingUnits { get; init; }
}
