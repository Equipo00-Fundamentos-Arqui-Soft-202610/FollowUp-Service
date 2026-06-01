using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IAdherenceHistoryQueryService
{
    Task<AdherenceHistory?> HandleAsync(GetAdherenceHistoryQuery query);
}
