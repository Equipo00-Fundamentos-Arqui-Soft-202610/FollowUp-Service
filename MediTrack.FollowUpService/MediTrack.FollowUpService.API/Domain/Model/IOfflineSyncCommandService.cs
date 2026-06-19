using MediTrack.FollowUpService.API.Domain.Model.Commands;

namespace MediTrack.FollowUpService.API.Domain.Model;

public record OfflineSyncResultDto(string EntityType, int? CreatedId, string Status, string? ErrorMessage);

public interface IOfflineSyncCommandService
{
    Task<List<OfflineSyncResultDto>> Handle(ProcessSyncBatchCommand command);
}
