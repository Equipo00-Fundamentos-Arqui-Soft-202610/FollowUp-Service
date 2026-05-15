namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record RecordComplianceResource(
    int PatientId,
    int DoseScheduleId,
    string Status,
    string? VideoUrl = null,
    DateTime? OfflineRecordedAt = null
);

public record MedicationComplianceResource(
    int Id,
    int PatientId,
    int DoseScheduleId,
    string Status,
    DateTime RecordedAt,
    string? VideoUrl,
    bool Synced,
    DateTime? OfflineRecordedAt
);