using MediTrack.FollowUpService.API.Domain.Model.Aggregates;

namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IMedicationComplianceRepository
{
    Task<MedicationCompliance?> FindByIdAsync(int complianceId);
    Task<ICollection<MedicationCompliance>> FindByPatientIdAsync(int patientId);
    Task<ICollection<MedicationCompliance>> FindByDoseScheduleIdAsync(int doseScheduleId);
    Task AddAsync(MedicationCompliance compliance);
    Task UpdateAsync(MedicationCompliance compliance);
    Task DeleteAsync(int complianceId);

    /// Bandeja del validador (MediTrack AI Validator — Prototype): todos los
    /// cumplimientos pendientes de validación, de cualquier paciente, con
    /// Medication cargada para poder mostrar nombre/dosis.
    Task<ICollection<MedicationCompliance>> FindPendingValidationAsync();

    /// Compliance de HOY (fecha calendario, mismo criterio que ya usa
    /// NextPendingDoseQueryService: RecordedAt.Date == today) para un doseSchedule
    /// dado que ya tenga un intento de evidencia (PendingValidation/Rejected) —
    /// usado para el upsert al volver a grabar el video en vez de acumular filas
    /// duplicadas.
    Task<MedicationCompliance?> FindTodayValidationAttemptAsync(int patientId, int doseScheduleId, DateTime today);
}