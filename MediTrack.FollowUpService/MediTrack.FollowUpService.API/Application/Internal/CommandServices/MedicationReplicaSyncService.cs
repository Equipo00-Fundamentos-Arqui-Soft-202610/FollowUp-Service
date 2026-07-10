using System.Globalization;
using MediTrack.FollowUpService.API.Domain.Model;
using MediTrack.FollowUpService.API.Domain.Model.Aggregates;
using MediTrack.FollowUpService.API.Domain.Models;

namespace MediTrack.FollowUpService.API.Application.Internal.CommandServices;

public class MedicationReplicaSyncService : IMedicationReplicaSyncService
{
    private readonly IMedicationRepository _medicationRepository;
    private readonly ITreatmentMedicationsClient _treatmentClient;
    private readonly ILogger<MedicationReplicaSyncService> _logger;

    public MedicationReplicaSyncService(
        IMedicationRepository medicationRepository,
        ITreatmentMedicationsClient treatmentClient,
        ILogger<MedicationReplicaSyncService> logger)
    {
        _medicationRepository = medicationRepository;
        _treatmentClient = treatmentClient;
        _logger = logger;
    }

    public async Task EnsureSyncedAsync(int patientId)
    {
        var existing = await _medicationRepository.FindByPatientIdAsync(patientId);
        if (existing.Count > 0)
        {
            _logger.LogDebug(
                "[next-dose-sync] patientId={PatientId}: réplica local ya tiene {Count} medicamentos, no se consulta Treatment-Service.",
                patientId, existing.Count);
            return;
        }

        _logger.LogWarning(
            "[next-dose-sync] patientId={PatientId}: réplica local VACÍA. Probable evento RabbitMQ " +
            "'PrescriptionCreated' nunca recibido (ver docs/next-dose-sync-fix.md). Consultando Treatment-Service como respaldo...",
            patientId);

        var treatmentMedications = await _treatmentClient.GetMedicationsByPatientIdAsync(patientId);
        var activeOnes = treatmentMedications.Where(m => m.IsActive).ToList();

        _logger.LogInformation(
            "[next-dose-sync] patientId={PatientId}: Treatment-Service devolvió {Total} medicamentos ({Active} activos).",
            patientId, treatmentMedications.Count, activeOnes.Count);

        foreach (var dto in activeOnes)
        {
            var medication = Medication.CreateFromEvent(
                id: dto.Id,
                patientId: patientId,
                name: dto.OfficialName,
                dose: dto.Dose,
                frequencyHours: dto.FrequencyHours,
                startDate: dto.StartDate,
                endDate: dto.EndDate,
                stockCount: dto.StockCount,
                stockAlertThreshold: dto.StockAlertThreshold);

            // Treatment-Service no expone el id real del DoseSchedule en este
            // endpoint (solo la hora formateada) — se asigna un id local
            // determinístico namespacing por medicationId. Es un identificador
            // técnico interno de FollowUp-Service, no un dato clínico inventado:
            // la hora/medicamento sí son los reales de Treatment-Service.
            // LIMITACIÓN CONOCIDA: en teoría podría colisionar con un id real
            // de otro medicamento sincronizado por evento; documentado en
            // docs/next-dose-sync-fix.md.
            for (var index = 0; index < dto.ScheduledTimes.Count; index++)
            {
                if (!TryParseScheduledTime(dto.ScheduledTimes[index], out var scheduledTime))
                {
                    _logger.LogWarning(
                        "[next-dose-sync] patientId={PatientId} medicationId={MedicationId}: no se pudo parsear scheduledTime '{Raw}', se omite ese horario.",
                        patientId, dto.Id, dto.ScheduledTimes[index]);
                    continue;
                }

                var syntheticId = dto.Id * 1000 + index;
                medication.Schedules.Add(DoseSchedule.CreateFromEvent(
                    id: syntheticId,
                    medicationId: dto.Id,
                    scheduledTime: scheduledTime,
                    isActive: true));
            }

            await _medicationRepository.AddAsync(medication);

            _logger.LogInformation(
                "[next-dose-sync] patientId={PatientId}: sincronizado medicationId={MedicationId} '{Name}' con {ScheduleCount} horarios.",
                patientId, dto.Id, dto.OfficialName, medication.Schedules.Count);
        }
    }

    private static bool TryParseScheduledTime(string raw, out TimeSpan scheduledTime)
    {
        return TimeSpan.TryParseExact(raw, @"hh\:mm", CultureInfo.InvariantCulture, out scheduledTime)
            || TimeSpan.TryParse(raw, CultureInfo.InvariantCulture, out scheduledTime);
    }
}
