using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Commands;
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
        // Validate that status is either "taken" or "skipped"
        if (command.Status != "taken" && command.Status != "skipped")
            throw new ArgumentException("Status must be either 'taken' or 'skipped'");

        // Validate that DoseSchedule exists
        var doseScheduleExists = await _context.DoseSchedules
            .AnyAsync(ds => ds.Id == command.DoseScheduleId);
        
        if (!doseScheduleExists)
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
