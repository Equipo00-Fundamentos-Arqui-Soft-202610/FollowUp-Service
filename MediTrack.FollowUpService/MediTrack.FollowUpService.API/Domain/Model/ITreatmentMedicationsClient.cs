namespace MediTrack.FollowUpService.API.Domain.Model;

/// Contrato real de Treatment-Service (`GET /api/v1/medications?patientId=`,
/// `MedicationResource`) — se replica aquí solo lo necesario para el
/// respaldo de sincronización. No inventa campos: son exactamente los que
/// expone ese endpoint público (sin autenticación) hoy.
public record TreatmentMedicationDto(
    int Id,
    int PrescriptionId,
    int CatalogId,
    string OfficialName,
    string? Category,
    string Dose,
    int FrequencyHours,
    DateTime StartDate,
    DateTime? EndDate,
    int StockCount,
    int StockAlertThreshold,
    bool IsActive,
    List<string> ScheduledTimes
);

/// Cliente de solo lectura hacia Treatment-Service — usado ÚNICAMENTE como
/// respaldo cuando la réplica local de FollowUp-Service (poblada por eventos
/// RabbitMQ) está vacía para un paciente. Ver MedicationReplicaSyncService.
public interface ITreatmentMedicationsClient
{
    Task<IReadOnlyList<TreatmentMedicationDto>> GetMedicationsByPatientIdAsync(int patientId);
}
