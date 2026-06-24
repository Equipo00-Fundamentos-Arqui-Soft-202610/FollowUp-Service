namespace MediTrack.FollowUpService.API.Application.OutboundEvents;

public record DoseScheduleCreatedDto(int DoseScheduleId, TimeSpan ScheduledTime, bool IsActive);

public record MedicationCreatedDto(
    int MedicationId, int PatientId, int PrescriptionId, int CatalogId,
    string MedicationName, string Dose, int FrequencyHours, DateTime StartDate,
    DateTime? EndDate, int StockCount, int StockAlertThreshold,
    List<DoseScheduleCreatedDto> DoseSchedules);

public record PrescriptionCreatedEvent
{
    public Guid EventId { get; init; }
    public DateTime OccurredAtUtc { get; init; }
    public int PrescriptionId { get; init; }
    public int PatientId { get; init; }
    public List<MedicationCreatedDto> Medications { get; init; } = new();
}
