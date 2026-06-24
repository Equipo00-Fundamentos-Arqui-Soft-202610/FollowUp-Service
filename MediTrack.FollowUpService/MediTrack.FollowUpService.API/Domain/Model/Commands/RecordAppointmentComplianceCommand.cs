namespace MediTrack.FollowUpService.API.Domain.Model.Commands;

public record RecordAppointmentComplianceCommand(
    int PatientId,
    int AppointmentId,
    bool Attended,
    string? Notes
);
