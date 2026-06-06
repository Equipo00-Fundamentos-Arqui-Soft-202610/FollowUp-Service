using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface ILowStockMedicationQueryService
{
    Task<ICollection<Medication>> HandleAsync(GetLowStockMedicationsQuery query);
}
