using MediTrack.FollowUpService.API.Application.Internal.QueryServices;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using MediTrack.FollowUpService.Tests.TestSupport;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MediTrack.FollowUpService.Tests;

/// Cubre la corrección del bug: antes del fix, "next-dose" solo excluía una
/// dosis cuando el compliance estaba en "taken"/"approved" (IsTaken), por lo
/// que una dosis registrada como "skipped" (no tomada) seguía devolviéndose
/// como disponible en la siguiente consulta.
public class NextPendingDoseQueryServiceTests
{
    private static NextPendingDoseQueryService BuildService(
        FollowUpDbContext context,
        MedicationRepository medicationRepository,
        MedicationComplianceRepository complianceRepository)
    {
        var fakeClient = new FakeTreatmentMedicationsClient(new List<TreatmentMedicationDto>());
        var syncService = new MediTrack.FollowUpService.API.Application.Internal.CommandServices.MedicationReplicaSyncService(
            medicationRepository, fakeClient, NullLogger<MediTrack.FollowUpService.API.Application.Internal.CommandServices.MedicationReplicaSyncService>.Instance);

        return new NextPendingDoseQueryService(
            medicationRepository, complianceRepository, syncService, NullLogger<NextPendingDoseQueryService>.Instance);
    }

    [Fact]
    public async Task HandleAsync_ConComplianceSkippedHoy_ExcluyeLaDosisYDevuelveNull()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        TestFixtures.SeedActiveDoseSchedule(context, patientId: 1, medicationId: 100, doseScheduleId: 200);

        var medicationRepository = new MedicationRepository(context);
        var complianceRepository = new MedicationComplianceRepository(context);
        await complianceRepository.AddAsync(new MedicationCompliance(1, 200, "skipped"));

        var queryService = BuildService(context, medicationRepository, complianceRepository);

        var result = await queryService.HandleAsync(new GetNextPendingDoseQuery(1));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_ConComplianceTakenHoy_SigueExcluyendoLaDosis()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        TestFixtures.SeedActiveDoseSchedule(context, patientId: 2, medicationId: 101, doseScheduleId: 201);

        var medicationRepository = new MedicationRepository(context);
        var complianceRepository = new MedicationComplianceRepository(context);
        await complianceRepository.AddAsync(new MedicationCompliance(2, 201, "taken"));

        var queryService = BuildService(context, medicationRepository, complianceRepository);

        var result = await queryService.HandleAsync(new GetNextPendingDoseQuery(2));

        Assert.Null(result);
    }

    [Fact]
    public async Task HandleAsync_SinComplianceHoy_DevuelveLaDosisComoPendiente()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        TestFixtures.SeedActiveDoseSchedule(context, patientId: 3, medicationId: 102, doseScheduleId: 202);

        var medicationRepository = new MedicationRepository(context);
        var complianceRepository = new MedicationComplianceRepository(context);

        var queryService = BuildService(context, medicationRepository, complianceRepository);

        var result = await queryService.HandleAsync(new GetNextPendingDoseQuery(3));

        Assert.NotNull(result);
        Assert.Equal(202, result!.DoseScheduleId);
    }
}
