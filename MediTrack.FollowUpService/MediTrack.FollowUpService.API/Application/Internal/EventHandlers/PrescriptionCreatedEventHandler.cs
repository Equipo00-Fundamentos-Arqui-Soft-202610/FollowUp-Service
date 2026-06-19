using MediTrack.FollowUpService.API.Application.OutboundEvents;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Models;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.API.Application.Internal.EventHandlers;

public class PrescriptionCreatedEventHandler : IPrescriptionCreatedEventHandler
{
    private readonly FollowUpDbContext _context;
    private readonly ILogger<PrescriptionCreatedEventHandler> _logger;

    public PrescriptionCreatedEventHandler(FollowUpDbContext context, ILogger<PrescriptionCreatedEventHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task HandleAsync(PrescriptionCreatedEvent evt)
    {
        foreach (var medDto in evt.Medications)
        {
            var existingMedication = await _context.Medications
                .Include(m => m.Schedules)
                .FirstOrDefaultAsync(m => m.Id == medDto.MedicationId);

            if (existingMedication is not null)
            {
                _logger.LogInformation(
                    "Medication {MedicationId} already exists locally, skipping insert",
                    medDto.MedicationId);
                continue;
            }

            var medication = Medication.CreateFromEvent(
                medDto.MedicationId, medDto.PatientId, medDto.MedicationName,
                medDto.Dose, medDto.FrequencyHours, medDto.StartDate, medDto.EndDate,
                medDto.StockCount, medDto.StockAlertThreshold);

            foreach (var scheduleDto in medDto.DoseSchedules)
            {
                var schedule = DoseSchedule.CreateFromEvent(
                    scheduleDto.DoseScheduleId, medication.Id,
                    scheduleDto.ScheduledTime, scheduleDto.IsActive);
                medication.Schedules.Add(schedule);
            }

            _context.Medications.Add(medication);
        }

        await _context.SaveChangesAsync();
    }
}
