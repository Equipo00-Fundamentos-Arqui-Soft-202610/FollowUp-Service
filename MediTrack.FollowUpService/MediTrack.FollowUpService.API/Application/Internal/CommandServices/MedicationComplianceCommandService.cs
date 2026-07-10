using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using MediTrack.FollowUpService.API.Infrastructure.Messaging;
using MediTrack.FollowUpService.API.Infrastructure.Persistence;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Application.Internal.CommandServices;

public class MedicationComplianceCommandService : IMedicationComplianceCommandService
{
    private readonly IMedicationComplianceRepository _complianceRepository;
    private readonly FollowUpDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<MedicationComplianceCommandService> _logger;

    public MedicationComplianceCommandService(
        IMedicationComplianceRepository complianceRepository,
        FollowUpDbContext context,
        IEventPublisher eventPublisher,
        ILogger<MedicationComplianceCommandService> logger)
    {
        _complianceRepository = complianceRepository;
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<MedicationCompliance> HandleAsync(RecordComplianceCommand command)
    {
        // Este endpoint (registro directo, sin evidencia) sigue reservado a
        // "taken"/"skipped": la validación real de valores permitidos vive en
        // ComplianceStatus.From (evita la duplicación que existía aquí).
        // Los estados del flujo de video (PendingValidation/Approved/Rejected)
        // se manejan por su propio endpoint en ComplianceVideoController.
        if (command.Status != ComplianceStatus.Taken.Value && command.Status != ComplianceStatus.Skipped.Value)
            throw new ArgumentException("Status must be either 'taken' or 'skipped'");

        // Validate that DoseSchedule exists and load Medication
        var doseSchedule = await _context.DoseSchedules
            .Include(ds => ds.Medication)
            .FirstOrDefaultAsync(ds => ds.Id == command.DoseScheduleId);
        
        if (doseSchedule is null)
            throw new ArgumentException($"DoseSchedule with ID {command.DoseScheduleId} does not exist");

        // Create compliance record
        var compliance = new MedicationCompliance(
            patientId: command.PatientId,
            doseScheduleId: command.DoseScheduleId,
            status: command.Status,
            videoUrl: command.VideoUrl,
            offlineRecordedAt: command.OfflineRecordedAt
        );

        await _complianceRepository.AddAsync(compliance);

        if (command.Status == "taken")
        {
            var medication = doseSchedule.Medication;
            var previousStock = medication.StockCount;
            var newStock = Math.Max(0, previousStock - 1);
            medication.UpdateStockCount(newStock);
            await _context.SaveChangesAsync();

            if (previousStock > medication.StockAlertThreshold && newStock <= medication.StockAlertThreshold)
            {
                try
                {
                    await _eventPublisher.PublishAsync("StockBajo", new StockBajoEvent
                    {
                        PatientId = medication.PatientId,
                        MedicationId = medication.Id,
                        MedicationName = medication.Name,
                        RemainingUnits = newStock
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to publish StockBajo event for MedicationId {MedicationId}",
                        medication.Id);
                }
            }
        }

        try
        {
            var wasCompliant = compliance.IsTaken;

            await _eventPublisher.PublishAsync(
                "CumplimientoRegistrado",
                new CumplimientoRegistradoEvent
                {
                    PatientId = compliance.PatientId,
                    EntityId = compliance.DoseScheduleId
                });

            await _eventPublisher.PublishAsync(
                "ComplianceRegistered",
                new ComplianceRegisteredEvent
                {
                    PatientId = compliance.PatientId,
                    WasCompliant = wasCompliant
                });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish compliance registered events for patient {PatientId} and dose schedule {DoseScheduleId}",
                compliance.PatientId,
                compliance.DoseScheduleId);
        }

        return compliance;
    }
}
