using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public static class AppointmentComplianceResourceFromEntityAssembler
{
    public static AppointmentComplianceResource ToResourceFromEntity(AppointmentCompliance entity)
    {
        return new AppointmentComplianceResource(
            entity.Id,
            entity.PatientId,
            entity.AppointmentId,
            entity.Attended,
            entity.RecordedAt,
            entity.Notes);
    }
}
