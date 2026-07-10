using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Models;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using Microsoft.EntityFrameworkCore;

namespace MediTrack.FollowUpService.Tests.TestSupport;

public static class TestFixtures
{
    public static FollowUpDbContext NewInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<FollowUpDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new FollowUpDbContext(options);
    }

    /// Crea un Medication + DoseSchedule activos y los persiste, devolviendo el DoseSchedule.
    public static DoseSchedule SeedActiveDoseSchedule(
        FollowUpDbContext context,
        int patientId = 1,
        int medicationId = 100,
        int doseScheduleId = 200,
        string medicationName = "Losartán",
        string dose = "50mg",
        TimeSpan? scheduledTime = null)
    {
        var medication = Medication.CreateFromEvent(
            id: medicationId,
            patientId: patientId,
            name: medicationName,
            dose: dose,
            frequencyHours: 24,
            startDate: DateTime.UtcNow.AddDays(-10),
            endDate: null,
            stockCount: 30,
            stockAlertThreshold: 5);

        var doseSchedule = DoseSchedule.CreateFromEvent(
            id: doseScheduleId,
            medicationId: medicationId,
            scheduledTime: scheduledTime ?? DateTime.UtcNow.TimeOfDay,
            isActive: true);

        medication.Schedules.Add(doseSchedule);

        context.Medications.Add(medication);
        context.DoseSchedules.Add(doseSchedule);
        context.SaveChanges();

        return doseSchedule;
    }
}
