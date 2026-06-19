using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Commands;
using MediTrack.FollowUpService.API.Infrastructure.Messaging;

namespace MediTrack.FollowUpService.API.Application.Internal.CommandServices;

public class AppointmentComplianceCommandService : IAppointmentComplianceCommandService
{
    private readonly IAppointmentComplianceRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<AppointmentComplianceCommandService> _logger;

    public AppointmentComplianceCommandService(
        IAppointmentComplianceRepository repository,
        IEventPublisher eventPublisher,
        ILogger<AppointmentComplianceCommandService> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<AppointmentCompliance?> Handle(RecordAppointmentComplianceCommand command)
    {
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
