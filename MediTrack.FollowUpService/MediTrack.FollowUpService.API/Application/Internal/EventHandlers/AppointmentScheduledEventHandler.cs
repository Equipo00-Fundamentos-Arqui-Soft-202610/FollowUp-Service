using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Application.Internal.EventHandlers;

public class AppointmentScheduledEventHandler : IAppointmentScheduledEventHandler
{
    private readonly FollowUpDbContext _context;
    private readonly ILogger<AppointmentScheduledEventHandler> _logger;

    public AppointmentScheduledEventHandler(FollowUpDbContext context, ILogger<AppointmentScheduledEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(AppointmentScheduledEvent evt)
    {
        var existing = await _context.AppointmentReferences
            .FirstOrDefaultAsync(a => a.Id == evt.AppointmentId);

        if (existing is not null)
        {
            _logger.LogInformation(
                "AppointmentReference {AppointmentId} already exists locally, skipping insert",
                evt.AppointmentId);
            return;
        }

        var reference = AppointmentReference.CreateFromEvent(
            evt.AppointmentId, evt.PatientId, evt.AppointmentDateUtc);

        _context.AppointmentReferences.Add(reference);
        await _context.SaveChangesAsync();
    }
}
