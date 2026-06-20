using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Infrastructure.Messaging;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Application.Internal.CommandServices;

public class AppointmentComplianceCommandService : IAppointmentComplianceCommandService
{
    private readonly IAppointmentComplianceRepository _repository;
    private readonly FollowUpDbContext _context;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<AppointmentComplianceCommandService> _logger;

    public AppointmentComplianceCommandService(
        IAppointmentComplianceRepository repository,
        FollowUpDbContext context,
        IEventPublisher eventPublisher,
        ILogger<AppointmentComplianceCommandService> logger)
    {
        _repository = repository;
        _context = context;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<AppointmentCompliance?> Handle(RecordAppointmentComplianceCommand command)
    {
        var appointmentReference = await _context.AppointmentReferences
            .FirstOrDefaultAsync(a => a.Id == command.AppointmentId);

        if (appointmentReference is null)
            throw new ArgumentException($"Appointment with ID {command.AppointmentId} does not exist");

        if (appointmentReference.PatientId != command.PatientId)
            throw new ArgumentException(
                $"Appointment {command.AppointmentId} does not belong to patient {command.PatientId}");

        var compliance = new AppointmentCompliance(
            command.PatientId,
            command.AppointmentId,
            command.Attended,
            command.Notes);

        await _repository.AddAsync(compliance);

        try
        {
            await _eventPublisher.PublishAsync(
                "ComplianceRegistered",
                new AppointmentComplianceRegisteredEvent
                {
                    PatientId = compliance.PatientId,
                    WasCompliant = compliance.Attended
                });
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to publish appointment compliance registered event for patient {PatientId} and appointment {AppointmentId}",
                compliance.PatientId,
                compliance.AppointmentId);
        }

        return compliance;
    }
}
