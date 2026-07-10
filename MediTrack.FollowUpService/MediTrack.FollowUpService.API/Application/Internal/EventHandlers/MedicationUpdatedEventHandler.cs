using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Application.Internal.EventHandlers;

public class MedicationUpdatedEventHandler : IMedicationUpdatedEventHandler
{
    private readonly FollowUpDbContext _context;
    private readonly ILogger<MedicationUpdatedEventHandler> _logger;

    public MedicationUpdatedEventHandler(FollowUpDbContext context, ILogger<MedicationUpdatedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(MedicationUpdatedEvent evt)
    {
        var medication = await _context.Medications
            .FirstOrDefaultAsync(m => m.Id == evt.MedicationId);

        if (medication is null)
        {
            _logger.LogWarning(
                "MedicationUpdated received for unknown MedicationId {MedicationId}, skipping",
                evt.MedicationId);
            return;
        }

        medication.Dose = new DoseValue(evt.Dose);
        medication.FrequencyHours = evt.FrequencyHours;
        medication.StartDate = evt.StartDate;
        medication.EndDate = evt.EndDate;
        medication.StockCount = evt.StockCount;
        medication.StockAlertThreshold = evt.StockAlertThreshold;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Medication {MedicationId} updated in Follow-up from MedicationUpdated event",
            evt.MedicationId);
    }
}
