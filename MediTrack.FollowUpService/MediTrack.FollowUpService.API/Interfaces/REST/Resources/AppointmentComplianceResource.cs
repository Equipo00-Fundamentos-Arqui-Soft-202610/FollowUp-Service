namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record AppointmentComplianceResource(
    int Id,
    int PatientId,
    int AppointmentId,
    bool Attended,
    DateTime RecordedAt,
    string? Notes
);
