namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

public record NextPendingDoseResource(
    int DoseScheduleId,
    string MedicationName,
    string Dose,
    string ScheduledTime,
    int MinutesUntilDose,
    // Instante real (UTC, ISO-8601) de la ocurrencia de hoy — el cliente lo usa
    // para calcular la ventana de toma sin horas hardcodeadas ni ambigüedad de zona horaria.
    string ScheduledAtUtc,
    // Si ya existe un intento de evidencia para la dosis de hoy (PendingValidation o
    // Rejected), se informa aquí para que el cliente derive el estado visual correcto.
    int? ComplianceId,
    string? ValidationStatus,
    string? RejectionReason
);
