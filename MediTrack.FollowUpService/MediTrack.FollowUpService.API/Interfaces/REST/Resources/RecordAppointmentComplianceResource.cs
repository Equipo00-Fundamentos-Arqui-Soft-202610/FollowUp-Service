namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record RecordAppointmentComplianceResource(
    int PatientId,
    int AppointmentId,
    bool Attended,
    string? Notes
);
