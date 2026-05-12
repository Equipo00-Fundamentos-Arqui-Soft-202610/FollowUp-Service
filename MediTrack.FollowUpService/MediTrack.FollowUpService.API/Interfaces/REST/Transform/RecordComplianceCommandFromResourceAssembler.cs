using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public class RecordComplianceCommandFromResourceAssembler
{
    public RecordComplianceCommand ToCommand(int patientId, RecordComplianceResource resource)
    {
        return new RecordComplianceCommand(
            PatientId: patientId,
            DoseScheduleId: resource.DoseScheduleId,
            Status: resource.Status,
            VideoUrl: resource.VideoUrl,
            OfflineRecordedAt: resource.OfflineRecordedAt
        );
    }
}