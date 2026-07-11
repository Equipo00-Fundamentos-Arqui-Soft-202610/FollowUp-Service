using System.Runtime.CompilerServices;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.ValueObjects;

// Permite que el proyecto de tests llame directamente al método interno de
// una sola pasada del worker (RunOnceAsync), sin depender del temporizador
// de 1 minuto de BackgroundService ni exponerlo como API pública.
[assembly: InternalsVisibleTo("MediTrack.FollowUpService.Tests")]

namespace MediTrack.FollowUpService.API.Infrastructure.Cleanup;

/// Cierra automáticamente, cada minuto, las ocurrencias de dosis de HOY que
/// llegaron a T+10 (`scheduledAtUtc` + [CloseOffset]) sin ningún cumplimiento
/// asociado, registrándolas como `skipped`. Es el mecanismo que garantiza el
/// cierre incluso con MediTrack-Mobile completamente cerrado — el mobile solo
/// refleja lo que este worker (u otra vía) ya confirmó, nunca registra
/// `skipped` por sí mismo (ver `DoseReminderCoordinator`), evitando una
/// escritura duplicada entre mobile y backend sobre la misma tabla.
///
/// No transiciona `Rejected` a `Skipped` — un intento rechazado ya cuenta
/// como cumplimiento real de esa ocurrencia (conserva su historial) y se
/// excluye igual que `Taken`/`Approved`/`PendingValidation`/`Skipped`.
public class StaleDoseExpirationService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    /// Debe coincidir con `AppConstants.doseReminderCloseOffsetMinutes` en
    /// MediTrack-Mobile (T+10) — es el mismo cierre de ventana, solo que
    /// aplicado del lado del backend para no depender de que el mobile esté
    /// abierto.
    private static readonly TimeSpan CloseOffset = TimeSpan.FromMinutes(10);

    private static readonly TimeZoneInfo LimaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleDoseExpirationService> _logger;

    public StaleDoseExpirationService(
        IServiceScopeFactory scopeFactory,
        ILogger<StaleDoseExpirationService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(CheckInterval);

        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el cierre de dosis vencidas");
            }
        } while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    /// Una sola pasada de cierre (usada también directamente por los tests,
    /// sin esperar al temporizador de 1 minuto de BackgroundService).
    internal async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var medicationRepository = scope.ServiceProvider.GetRequiredService<IMedicationRepository>();
        var complianceRepository = scope.ServiceProvider.GetRequiredService<IMedicationComplianceRepository>();

        var nowUtc = DateTime.UtcNow;
        var nowLima = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, LimaTimeZone);
        var today = nowLima.Date;

        var activeMedications = await medicationRepository.FindAllActiveAsync();
        var expiredCount = 0;

        foreach (var medication in activeMedications)
        {
            foreach (var schedule in medication.Schedules.Where(s => s.IsActive))
            {
                var scheduledLocal = today.Add(schedule.ScheduledTime.Value);
                var scheduledUtc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(scheduledLocal, DateTimeKind.Unspecified),
                    LimaTimeZone);
                var closeAtUtc = scheduledUtc.Add(CloseOffset);

                if (nowUtc < closeAtUtc)
                    continue;

                try
                {
                    var created = await TryExpireOccurrenceAsync(
                        complianceRepository, medication.PatientId, schedule.Id, today);
                    if (created)
                        expiredCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error cerrando la ocurrencia de hoy de doseScheduleId={DoseScheduleId} (patientId={PatientId})",
                        schedule.Id, medication.PatientId);
                }
            }
        }

        if (expiredCount > 0)
            _logger.LogInformation("Se cerraron {Count} dosis vencidas como no tomadas (skipped)", expiredCount);
    }

    /// Recomprueba, justo antes de insertar, que la ocurrencia de HOY siga
    /// sin ningún cumplimiento real — evita el caso más común de duplicado
    /// (dos ocurrencias del propio bucle de este worker, o una carrera con
    /// el mobile/otro proceso que se resolvió justo en el intervalo entre el
    /// filtro inicial y este punto). Esto NO es una garantía transaccional
    /// a nivel de base de datos: dos INSTANCIAS del backend corriendo a la
    /// vez podrían, en teoría, pasar ambas esta comprobación antes de que
    /// cualquiera confirme su escritura. Cerrar esa ventana por completo
    /// requeriría un índice único (PatientId, DoseScheduleId, fecha) —
    /// explícitamente NO autorizado todavía (ver informe: hay que verificar
    /// primero cómo conviven los reintentos de evidencia con ese índice).
    private static async Task<bool> TryExpireOccurrenceAsync(
        IMedicationComplianceRepository complianceRepository,
        int patientId,
        int doseScheduleId,
        DateTime today)
    {
        var todaysAttempts = await complianceRepository.FindByDoseScheduleIdAsync(doseScheduleId);
        var alreadyResolved = todaysAttempts.Any(c => ToLimaDate(c.RecordedAt) == today);
        if (alreadyResolved)
            return false;

        var compliance = new MedicationCompliance(patientId, doseScheduleId, ComplianceStatus.Skipped.Value);
        await complianceRepository.AddAsync(compliance);
        return true;
    }

    private static DateTime ToLimaDate(DateTime utcDateTime)
    {
        var utc = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, LimaTimeZone).Date;
    }
}
