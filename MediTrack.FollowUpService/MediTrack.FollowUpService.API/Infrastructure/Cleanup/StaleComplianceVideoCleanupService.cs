using MediTrack.FollowUpService.API.Domain.Model;

namespace MediTrack.FollowUpService.API.Infrastructure.Cleanup;

/// Limpieza periódica de evidencia en video vencida (MediTrack AI Validator —
/// Prototype). Un caso `PendingValidation` cuyo `VideoExpiresAt` ya pasó (más de
/// 24h sin que un validador lo revise) se transiciona a `Rejected` con un motivo
/// explícito y se borra el archivo — así ningún caso queda "zombie" sin
/// resolución y se conserva el registro/resultado, tal como el resto de
/// cumplimientos ya validados.
public class StaleComplianceVideoCleanupService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private const string ExpiredRejectionReason = "Video expirado sin validación (más de 24 horas pendiente).";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StaleComplianceVideoCleanupService> _logger;

    public StaleComplianceVideoCleanupService(
        IServiceScopeFactory scopeFactory,
        ILogger<StaleComplianceVideoCleanupService> logger)
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
                await CleanupStaleVideosAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante la limpieza de videos de cumplimiento vencidos");
            }
        } while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task CleanupStaleVideosAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMedicationComplianceRepository>();
        var videoStorage = scope.ServiceProvider.GetRequiredService<ITemporaryVideoStorage>();

        var pending = await repository.FindPendingValidationAsync();
        var now = DateTime.UtcNow;

        var stale = pending.Where(mc => mc.VideoExpiresAt.HasValue && mc.VideoExpiresAt.Value < now).ToList();
        if (stale.Count == 0)
            return;

        foreach (var compliance in stale)
        {
            if (!string.IsNullOrEmpty(compliance.TemporaryVideoPath))
                videoStorage.Delete(compliance.TemporaryVideoPath);

            compliance.Reject(validatorId: null, rejectionReason: ExpiredRejectionReason);
            await repository.UpdateAsync(compliance);
        }

        _logger.LogInformation("Se expiraron {Count} evidencias pendientes de más de 24 horas", stale.Count);
    }
}
