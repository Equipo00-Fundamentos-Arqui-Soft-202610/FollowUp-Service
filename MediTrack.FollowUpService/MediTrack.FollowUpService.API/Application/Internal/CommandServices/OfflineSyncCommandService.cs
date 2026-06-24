using System.Text.Json;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Interfaces.REST.Resources;

namespace MediTrack.FollowUpService.API.Application.Internal.CommandServices;

public class OfflineSyncCommandService : IOfflineSyncCommandService
{
    private readonly IOfflineSyncQueueRepository _syncRepository;
    private readonly IMedicationComplianceCommandService _medicationComplianceService;
    private readonly IAppointmentComplianceCommandService _appointmentComplianceService;
    private readonly ILogger<OfflineSyncCommandService> _logger;

    public OfflineSyncCommandService(
        IOfflineSyncQueueRepository syncRepository,
        IMedicationComplianceCommandService medicationComplianceService,
        IAppointmentComplianceCommandService appointmentComplianceService,
        ILogger<OfflineSyncCommandService> logger)
    {
        _syncRepository = syncRepository;
        _medicationComplianceService = medicationComplianceService;
        _appointmentComplianceService = appointmentComplianceService;
        _logger = logger;
    }

    public async Task<List<OfflineSyncResultDto>> Handle(ProcessSyncBatchCommand command)
    {
        var results = new List<OfflineSyncResultDto>();

        foreach (var item in command.Items)
        {
            var queueItem = new OfflineSyncQueueItem(
                item.PatientId, item.EntityType, item.PayloadJson, item.QueuedAt);

            await _syncRepository.AddAsync(queueItem);

            try
            {
                int? createdId = null;

                switch (item.EntityType)
                {
                    case "medication_compliance":
                    {
                        var resource = JsonSerializer.Deserialize<RecordComplianceResource>(
                            item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (resource is null)
                        {
                            queueItem.MarkFailed();
                            await _syncRepository.UpdateAsync(queueItem);
                            results.Add(new OfflineSyncResultDto(item.EntityType, null, "failed", "Invalid payload format"));
                            continue;
                        }

                        var recordCommand = new RecordComplianceCommand(
                            item.PatientId,
                            resource.DoseScheduleId,
                            resource.Status,
                            resource.VideoUrl,
                            resource.OfflineRecordedAt);

                        var compliance = await _medicationComplianceService.HandleAsync(recordCommand);
                        createdId = compliance.Id;
                        break;
                    }

                    case "appointment_compliance":
                    {
                        var resource = JsonSerializer.Deserialize<RecordAppointmentComplianceResource>(
                            item.PayloadJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (resource is null)
                        {
                            queueItem.MarkFailed();
                            await _syncRepository.UpdateAsync(queueItem);
                            results.Add(new OfflineSyncResultDto(item.EntityType, null, "failed", "Invalid payload format"));
                            continue;
                        }

                        var apptCommand = new RecordAppointmentComplianceCommand(
                            resource.PatientId,
                            resource.AppointmentId,
                            resource.Attended,
                            resource.Notes);

                        var appointmentCompliance = await _appointmentComplianceService.Handle(apptCommand);
                        createdId = appointmentCompliance?.Id;
                        break;
                    }

                    default:
                    {
                        queueItem.MarkFailed();
                        await _syncRepository.UpdateAsync(queueItem);
                        results.Add(new OfflineSyncResultDto(
                            item.EntityType, null, "failed", $"Unknown entity type: {item.EntityType}"));
                        continue;
                    }
                }

                queueItem.MarkSynced();
                await _syncRepository.UpdateAsync(queueItem);
                results.Add(new OfflineSyncResultDto(item.EntityType, createdId, "synced", null));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to process sync item for patient {PatientId}, type {EntityType}",
                    item.PatientId, item.EntityType);

                queueItem.MarkFailed();
                await _syncRepository.UpdateAsync(queueItem);
                results.Add(new OfflineSyncResultDto(item.EntityType, null, "failed", ex.Message));
            }
        }

        return results;
    }
}
