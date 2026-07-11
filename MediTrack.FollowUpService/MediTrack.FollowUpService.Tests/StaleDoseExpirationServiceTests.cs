using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Infrastructure.Cleanup;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC;
using MediTrack.FollowUpService.API.Infrastructure.Persistence.EFC.Configuration;
using MediTrack.FollowUpService.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MediTrack.FollowUpService.Tests;

/// Cubre `StaleDoseExpirationService`: cierra a T+10 (sin ningún cumplimiento)
/// registrando `skipped`, es idempotente entre corridas, y respeta cualquier
/// cumplimiento real ya existente (taken/approved/pendingvalidation/rejected/
/// skipped) sin crear un segundo registro.
public class StaleDoseExpirationServiceTests
{
    private static readonly TimeZoneInfo LimaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    /// Hora local (Lima) de hoy que ya venció hace más de 10 minutos.
    private static TimeSpan _ExpiredScheduledTime()
    {
        var nowLima = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LimaTimeZone);
        var scheduled = nowLima.AddMinutes(-11);
        return scheduled.TimeOfDay;
    }

    private static IServiceScopeFactory BuildScopeFactory(string dbName)
    {
        var services = new ServiceCollection();
        services.AddDbContext<FollowUpDbContext>(options => options.UseInMemoryDatabase(dbName));
        services.AddScoped<IMedicationRepository, MedicationRepository>();
        services.AddScoped<IMedicationComplianceRepository, MedicationComplianceRepository>();
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static FollowUpDbContext NewContextFor(string dbName)
    {
        var options = new DbContextOptionsBuilder<FollowUpDbContext>().UseInMemoryDatabase(dbName).Options;
        return new FollowUpDbContext(options);
    }

    [Fact]
    public async Task RunOnceAsync_SinNingunCumplimiento_CreaUnSoloSkipped()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seedContext = NewContextFor(dbName))
        {
            TestFixtures.SeedActiveDoseSchedule(
                seedContext, patientId: 1, medicationId: 100, doseScheduleId: 200,
                scheduledTime: _ExpiredScheduledTime());
        }

        var service = new StaleDoseExpirationService(BuildScopeFactory(dbName), NullLogger<StaleDoseExpirationService>.Instance);
        await service.RunOnceAsync(CancellationToken.None);

        await using var verifyContext = NewContextFor(dbName);
        var compliances = await new MedicationComplianceRepository(verifyContext).FindByDoseScheduleIdAsync(200);

        var compliance = Assert.Single(compliances);
        Assert.Equal("skipped", compliance.Status.Value);
        Assert.Equal(1, compliance.PatientId);
    }

    [Theory]
    [InlineData("taken")]
    [InlineData("approved")]
    [InlineData("pendingvalidation")]
    [InlineData("rejected")]
    [InlineData("skipped")]
    public async Task RunOnceAsync_ConCualquierCumplimientoYaExistente_NoCreaOtroRegistro(string existingStatus)
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seedContext = NewContextFor(dbName))
        {
            TestFixtures.SeedActiveDoseSchedule(
                seedContext, patientId: 1, medicationId: 100, doseScheduleId: 200,
                scheduledTime: _ExpiredScheduledTime());
            seedContext.MedicationCompliances.Add(new MedicationCompliance(1, 200, existingStatus));
            seedContext.SaveChanges();
        }

        var service = new StaleDoseExpirationService(BuildScopeFactory(dbName), NullLogger<StaleDoseExpirationService>.Instance);
        await service.RunOnceAsync(CancellationToken.None);

        await using var verifyContext = NewContextFor(dbName);
        var compliances = await new MedicationComplianceRepository(verifyContext).FindByDoseScheduleIdAsync(200);

        var compliance = Assert.Single(compliances);
        // Se conserva el estado real ya registrado — nunca se convierte
        // (p.ej. Rejected nunca pasa a Skipped) ni se duplica.
        Assert.Equal(existingStatus, compliance.Status.Value);
    }

    [Fact]
    public async Task RunOnceAsync_DosCorridasConsecutivas_NoDuplicaElRegistro()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seedContext = NewContextFor(dbName))
        {
            TestFixtures.SeedActiveDoseSchedule(
                seedContext, patientId: 1, medicationId: 100, doseScheduleId: 200,
                scheduledTime: _ExpiredScheduledTime());
        }

        var service = new StaleDoseExpirationService(BuildScopeFactory(dbName), NullLogger<StaleDoseExpirationService>.Instance);
        await service.RunOnceAsync(CancellationToken.None);
        await service.RunOnceAsync(CancellationToken.None);

        await using var verifyContext = NewContextFor(dbName);
        var compliances = await new MedicationComplianceRepository(verifyContext).FindByDoseScheduleIdAsync(200);

        Assert.Single(compliances);
    }

    [Fact]
    public async Task RunOnceAsync_ConHorarioAunNoVencido_NoCreaNada()
    {
        var dbName = Guid.NewGuid().ToString();
        var nowLima = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LimaTimeZone);
        await using (var seedContext = NewContextFor(dbName))
        {
            // Programada dentro de 2 horas: todavía muy lejos de T+10.
            TestFixtures.SeedActiveDoseSchedule(
                seedContext, patientId: 1, medicationId: 100, doseScheduleId: 200,
                scheduledTime: nowLima.AddHours(2).TimeOfDay);
        }

        var service = new StaleDoseExpirationService(BuildScopeFactory(dbName), NullLogger<StaleDoseExpirationService>.Instance);
        await service.RunOnceAsync(CancellationToken.None);

        await using var verifyContext = NewContextFor(dbName);
        var compliances = await new MedicationComplianceRepository(verifyContext).FindByDoseScheduleIdAsync(200);

        Assert.Empty(compliances);
    }
}
