using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public static class RecordAppointmentComplianceCommandFromResourceAssembler
{
    public static RecordAppointmentComplianceCommand ToCommandFromResource(RecordAppointmentComplianceResource resource)
    {
        return new RecordAppointmentComplianceCommand(
            resource.PatientId,
            resource.AppointmentId,
            resource.Attended,
            resource.Notes);
    }
}
