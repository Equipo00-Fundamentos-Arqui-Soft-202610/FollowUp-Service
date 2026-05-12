using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Interfaces.REST.Transform;

public class MedicationComplianceResourceFromEntityAssembler
{
    public MedicationComplianceResource ToResource(MedicationCompliance compliance)
    {
        return new MedicationComplianceResource(
            Id: compliance.Id,
            PatientId: compliance.PatientId,
            DoseScheduleId: compliance.DoseScheduleId,
            Status: compliance.Status,
            RecordedAt: compliance.RecordedAt,
            VideoUrl: compliance.VideoUrl,
            Synced: compliance.Synced,
            OfflineRecordedAt: compliance.OfflineRecordedAt
        );
    }

    public ICollection<MedicationComplianceResource> ToResources(
        ICollection<MedicationCompliance> compliances)
    {
        return compliances
            .Select(ToResource)
            .ToList();
    }
}