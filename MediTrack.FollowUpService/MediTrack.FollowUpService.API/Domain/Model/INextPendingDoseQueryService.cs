using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface INextPendingDoseQueryService
{
    Task<MedicationCompliance?> HandleAsync(GetNextPendingDoseQuery query);
}

