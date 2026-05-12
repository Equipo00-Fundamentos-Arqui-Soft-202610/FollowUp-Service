using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IMedicationQueryService
{
    Task<ICollection<Medication>> HandleAsync(GetMedicationsByPatientIdQuery query);
}