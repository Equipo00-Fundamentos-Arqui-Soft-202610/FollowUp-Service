using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Model.Queries;

namespace MediTrack.FollowUpService.API.Application.Internal.QueryServices;

public class NextPendingDoseQueryService : INextPendingDoseQueryService
{
    private readonly IMedicationRepository _medicationRepository;
    private readonly IMedicationComplianceRepository _complianceRepository;
    private readonly IMedicationReplicaSyncService _replicaSyncService;
    private readonly ILogger<NextPendingDoseQueryService> _logger;

    public NextPendingDoseQueryService(
        IMedicationRepository medicationRepository,
        IMedicationComplianceRepository complianceRepository,
        IMedicationReplicaSyncService replicaSyncService,
        ILogger<NextPendingDoseQueryService> logger)
    {
        _medicationRepository = medicationRepository;
        _complianceRepository = complianceRepository;
        _replicaSyncService = replicaSyncService;
        _logger = logger;
    }

    public async Task<MedicationCompliance?> HandleAsync(GetNextPendingDoseQuery query)
    {
        if (query.PatientId <= 0)
            throw new ArgumentException("PatientId must be greater than 0");

        _logger.LogInformation("[next-dose] patientId={PatientId}: consultando próxima dosis...", query.PatientId);

        // Respaldo: si la réplica local (poblada por eventos RabbitMQ) está
        // vacía para este paciente, se sincroniza desde Treatment-Service
        // antes de seguir. Ver docs/next-dose-sync-fix.md.
        await _replicaSyncService.EnsureSyncedAsync(query.PatientId);

        // Get all active medications for the patient
        var medications = await _medicationRepository.FindByPatientIdAsync(query.PatientId);
        var activeMedications = medications.Where(m => m.IsActive).ToList();

        _logger.LogInformation(
            "[next-dose] patientId={PatientId}: {Total} medicamentos encontrados, {Active} activos.",
            query.PatientId, medications.Count, activeMedications.Count);

        if (!activeMedications.Any())
        {
            _logger.LogWarning(
                "[next-dose] patientId={PatientId}: sin medicamentos activos, devolviendo null (404).",
                query.PatientId);
            return null;
        }

        // Get all active dose schedules from active medications
        var activeDoseSchedules = activeMedications
            .SelectMany(m => m.Schedules)
            .Where(s => s.IsActive)
            .ToList();

        _logger.LogInformation(
            "[next-dose] patientId={PatientId}: {Count} horarios de dosis activos encontrados.",
            query.PatientId, activeDoseSchedules.Count);

        if (!activeDoseSchedules.Any())
        {
            _logger.LogWarning(
                "[next-dose] patientId={PatientId}: medicamentos activos pero sin horarios activos, devolviendo null (404).",
                query.PatientId);
            return null;
        }

        // Get Lima Hour
        var limaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, limaTimeZone);
        var today = now.Date;

        _logger.LogInformation(
            "[next-dose] patientId={PatientId}: nowLima={NowLima:o} todayLima={Today:d}",
            query.PatientId, now, today);

        // Get all compliance records for today
        var complianceRecords = await _complianceRepository.FindByPatientIdAsync(query.PatientId);
        var todayCompliances = complianceRecords
            .Where(c => c.RecordedAt.Date == today)
            .ToList();

        // Se excluye la dosis si ya está resuelta como cumplida (legado "taken" o
        // validada "approved" — cubierto por IsTaken) o si ya quedó registrada
        // como "skipped" (no tomada, cierre definitivo del ciclo de recordatorios).
        // Si hay un intento "PendingValidation" o "Rejected" de hoy, la dosis SIGUE
        // devolviéndose como "next dose" (para que el cliente muestre "en
        // validación"/"rechazada" y no pueda re-enviar otra dosis distinta), solo
        // que ya no se ofrece como disponible para un envío nuevo sin evidencia.
        var nextPendingSchedule = activeDoseSchedules
            .Where(s =>
            {
                var hasResolvedCompliance = todayCompliances.Any(c =>
                    c.DoseScheduleId == s.Id && (c.Status.IsTaken || c.Status.IsSkipped));
                return !hasResolvedCompliance;
            })
            .OrderBy(s => s.ScheduledTime.Value > now.TimeOfDay ? 0 : 1)
            .ThenBy(s => s.ScheduledTime.Value)
            .FirstOrDefault();

        if (nextPendingSchedule == null)
        {
            _logger.LogWarning(
                "[next-dose] patientId={PatientId}: todos los horarios de hoy ya están resueltos (taken/approved/skipped), devolviendo null (404).",
                query.PatientId);
            return null;
        }

        // Si ya existe un intento de evidencia (pendiente o rechazado) de hoy para
        // esta dosis, se adjunta su información para que el resource/cliente sepa
        // en qué estado de validación está.
        var todayAttempt = todayCompliances.FirstOrDefault(c =>
            c.DoseScheduleId == nextPendingSchedule.Id &&
            (c.Status.IsPendingValidation || c.Status.IsRejected));

        _logger.LogInformation(
            "[next-dose] patientId={PatientId}: elegido doseScheduleId={DoseScheduleId} scheduledTime={ScheduledTime} " +
            "complianceId={ComplianceId} validationStatus={ValidationStatus}",
            query.PatientId, nextPendingSchedule.Id, nextPendingSchedule.ScheduledTime.Value,
            todayAttempt?.Id, todayAttempt?.Status?.Value);

        // Create a virtual MedicationCompliance object to return dose information
        // The DoseSchedule already has the Medication relationship loaded
        var doseCompliance = new MedicationCompliance
        {
            Id = todayAttempt?.Id ?? 0,
            DoseScheduleId = nextPendingSchedule.Id,
            DoseSchedule = nextPendingSchedule,
            PatientId = query.PatientId,
            Status = todayAttempt?.Status!,
            RejectionReason = todayAttempt?.RejectionReason,
        };

        return doseCompliance;
    }
}
