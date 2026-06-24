using MediTrack.FollowUpService.API.Domain.Model.Aggregates;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IOfflineSyncQueueRepository
{
    Task AddAsync(OfflineSyncQueueItem item);
    Task UpdateAsync(OfflineSyncQueueItem item);
    Task<IEnumerable<OfflineSyncQueueItem>> FindByPatientIdAsync(int patientId);
}
