namespace MediTrack.FollowUpService.API.Domain.Model.Commands;

public record RecordComplianceCommand(
    int PatientId,
    int DoseScheduleId,
    string Status,
    string? VideoUrl = null,
    DateTime? OfflineRecordedAt = null
);