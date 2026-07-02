using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Application.Internal.EventHandlers;

public class MedicationCancelledEventHandler : IMedicationCancelledEventHandler
{
    private readonly FollowUpDbContext _context;
    private readonly ILogger<MedicationCancelledEventHandler> _logger;

    public MedicationCancelledEventHandler(FollowUpDbContext context, ILogger<MedicationCancelledEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(MedicationCancelledEvent evt)
    {
        var medication = await _context.Medications
            .FirstOrDefaultAsync(m => m.Id == evt.MedicationId);

        if (medication is null)
        {
            _logger.LogWarning(
                "MedicationCancelled received for unknown MedicationId {MedicationId}, skipping",
                evt.MedicationId);
            return;
        }

        medication.EndDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Medication {MedicationId} marked as cancelled (EndDate = UtcNow) in Follow-up",
            evt.MedicationId);
    }
}
