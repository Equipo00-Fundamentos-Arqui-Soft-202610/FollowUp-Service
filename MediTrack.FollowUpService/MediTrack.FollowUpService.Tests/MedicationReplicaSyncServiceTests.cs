using MediTrack.FollowUpService.API.Application.Internal.CommandServices;
using MediTrack.FollowUpService.API.Application.Internal.QueryServices;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Queries;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MediTrack.FollowUpService.Tests;

/// Cubre la corrección del bug reportado: la réplica local de FollowUp-Service
/// (poblada solo por eventos RabbitMQ) puede quedar vacía si el evento
/// "PrescriptionCreated" se perdió — este respaldo la completa desde
/// Treatment-Service usando datos reales, no inventados.
public class MedicationReplicaSyncServiceTests
{
    [Fact]
    public async Task EnsureSyncedAsync_ConReplicaYaPoblada_NoConsultaTreatmentService()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        TestFixtures.SeedActiveDoseSchedule(context, patientId: 1, medicationId: 100, doseScheduleId: 200);

        var repository = new MedicationRepository(context);
        var fakeClient = new FakeTreatmentMedicationsClient(new List<TreatmentMedicationDto>());
        var syncService = new MedicationReplicaSyncService(
            repository, fakeClient, NullLogger<MedicationReplicaSyncService>.Instance);

        await syncService.EnsureSyncedAsync(1);

        Assert.Equal(0, fakeClient.CallCount);
    }

    [Fact]
    public async Task EnsureSyncedAsync_ConReplicaVacia_SincronizaSoloMedicamentosActivosDesdeTreatment()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        var repository = new MedicationRepository(context);

        var treatmentMedications = new List<TreatmentMedicationDto>
        {
            new(Id: 501, PrescriptionId: 1, CatalogId: 1, OfficialName: "Losartán", Category: "Cardio",
                Dose: "50mg", FrequencyHours: 24, StartDate: DateTime.UtcNow.AddDays(-5), EndDate: null,
                StockCount: 20, StockAlertThreshold: 5, IsActive: true,
                ScheduledTimes: new List<string> { "08:00", "20:00" }),
            new(Id: 502, PrescriptionId: 1, CatalogId: 2, OfficialName: "Ibuprofeno", Category: "Analgésico",
                Dose: "400mg", FrequencyHours: 12, StartDate: DateTime.UtcNow.AddDays(-40), EndDate: DateTime.UtcNow.AddDays(-10),
                StockCount: 0, StockAlertThreshold: 5, IsActive: false, // inactivo: no debe sincronizarse
                ScheduledTimes: new List<string> { "09:00" }),
        };

        var fakeClient = new FakeTreatmentMedicationsClient(treatmentMedications);
        var syncService = new MedicationReplicaSyncService(
            repository, fakeClient, NullLogger<MedicationReplicaSyncService>.Instance);

        await syncService.EnsureSyncedAsync(patientId: 7);

        var synced = await repository.FindByPatientIdAsync(7);
        Assert.Single(synced);
        var medication = synced.First();
        Assert.Equal(501, medication.Id);
        Assert.Equal("Losartán", medication.Name);
        Assert.Equal(2, medication.Schedules.Count);
        Assert.Contains(medication.Schedules, s => s.ScheduledTime.Value == new TimeSpan(8, 0, 0));
        Assert.Contains(medication.Schedules, s => s.ScheduledTime.Value == new TimeSpan(20, 0, 0));
        Assert.Equal(1, fakeClient.CallCount);
    }

    [Fact]
    public async Task NextPendingDoseQueryService_ConReplicaVacia_ResuelveTrasSincronizarDesdeTreatment()
    {
        // Regresión directa del bug reportado: antes del fix, con la réplica
        // local vacía, next-dose devolvía null (404) aunque Treatment-Service
        // sí tuviera el medicamento activo con horario.
        await using var context = TestFixtures.NewInMemoryContext();
        var medicationRepository = new MedicationRepository(context);
        var complianceRepository = new MedicationComplianceRepository(context);

        var inFiveMinutes = DateTime.UtcNow.AddMinutes(5);
        var limaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        var scheduledTimeLima = TimeZoneInfo.ConvertTimeFromUtc(inFiveMinutes, limaTimeZone).TimeOfDay;

        var treatmentMedications = new List<TreatmentMedicationDto>
        {
            new(Id: 900, PrescriptionId: 1, CatalogId: 1, OfficialName: "Metformina", Category: "Diabetes",
                Dose: "850mg", FrequencyHours: 24, StartDate: DateTime.UtcNow.AddDays(-1), EndDate: null,
                StockCount: 10, StockAlertThreshold: 2, IsActive: true,
                ScheduledTimes: new List<string> { scheduledTimeLima.ToString(@"hh\:mm") }),
        };

        var fakeClient = new FakeTreatmentMedicationsClient(treatmentMedications);
        var syncService = new MedicationReplicaSyncService(
            medicationRepository, fakeClient, NullLogger<MedicationReplicaSyncService>.Instance);
        var queryService = new NextPendingDoseQueryService(
            medicationRepository, complianceRepository, syncService, NullLogger<NextPendingDoseQueryService>.Instance);

        var result = await queryService.HandleAsync(new GetNextPendingDoseQuery(42));

        Assert.NotNull(result);
        Assert.Equal(900, result!.DoseSchedule.MedicationId);
        Assert.Equal(1, fakeClient.CallCount);
    }
}
