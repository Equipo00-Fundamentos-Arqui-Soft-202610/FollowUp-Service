using MediTrack.FollowUpService.API.Application.Internal.QueryServices;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.Tests.TestSupport;
using Xunit;

namespace MediTrack.FollowUpService.Tests;

/// Audita (sin cambiar) la regla real de adherencia: `WeeklyAdherence.TakenDoses`
/// solo cuenta compliances con `Status.IsTaken` (taken/approved) — confirmado
/// leyendo `AdherenceHistoryQueryService.HandleAsync` línea 40. Estos tests
/// documentan y verifican ese comportamiento ya existente para cada estado
/// real de `ComplianceStatus`, sin modificar la lógica de producción.
public class AdherenceHistoryQueryServiceTests
{
    private static async Task<decimal> PercentageForSingleComplianceAsync(string status)
    {
        await using var context = TestFixtures.NewInMemoryContext();
        TestFixtures.SeedActiveDoseSchedule(context, patientId: 1, medicationId: 100, doseScheduleId: 200);

        var medicationRepository = new MedicationRepository(context);
        var complianceRepository = new MedicationComplianceRepository(context);
        await complianceRepository.AddAsync(new MedicationCompliance(1, 200, status));

        var service = new AdherenceHistoryQueryService(complianceRepository, medicationRepository);
        var result = await service.HandleAsync(new GetAdherenceHistoryQuery(1));

        return result!.OverallAdherencePercentage;
    }

    [Fact]
    public async Task HandleAsync_ConComplianceTaken_CuentaComoAdherente()
    {
        var percentage = await PercentageForSingleComplianceAsync("taken");
        Assert.True(percentage > 0);
    }

    [Fact]
    public async Task HandleAsync_ConComplianceApproved_CuentaComoAdherente()
    {
        var percentage = await PercentageForSingleComplianceAsync("approved");
        Assert.True(percentage > 0);
    }

    [Fact]
    public async Task HandleAsync_ConComplianceSkipped_NoCuentaComoAdherente()
    {
        var percentage = await PercentageForSingleComplianceAsync("skipped");
        Assert.Equal(0m, percentage);
    }

    [Fact]
    public async Task HandleAsync_ConCompliancePendingValidation_NoCuentaTodaviaComoAdherente()
    {
        var percentage = await PercentageForSingleComplianceAsync("pendingvalidation");
        Assert.Equal(0m, percentage);
    }

    [Fact]
    public async Task HandleAsync_ConComplianceRejected_NoCuentaComoAdherente()
    {
        var percentage = await PercentageForSingleComplianceAsync("rejected");
        Assert.Equal(0m, percentage);
    }

    [Fact]
    public async Task HandleAsync_TakenYSkippedEnLaMismaSemana_SoloElTakenCuentaEnElNumerador()
    {
        await using var context = TestFixtures.NewInMemoryContext();
        TestFixtures.SeedActiveDoseSchedule(context, patientId: 2, medicationId: 101, doseScheduleId: 201);

        var medicationRepository = new MedicationRepository(context);
        var complianceRepository = new MedicationComplianceRepository(context);
        await complianceRepository.AddAsync(new MedicationCompliance(2, 201, "taken"));
        await complianceRepository.AddAsync(new MedicationCompliance(2, 201, "skipped"));

        var service = new AdherenceHistoryQueryService(complianceRepository, medicationRepository);
        var result = await service.HandleAsync(new GetAdherenceHistoryQuery(2));

        var week = Assert.Single(result!.Weeks);
        Assert.Equal(1, week.TakenDoses);
    }
}
