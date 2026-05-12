using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Commands;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IMedicationComplianceCommandService
{
    Task<MedicationCompliance> HandleAsync(RecordComplianceCommand command);
}