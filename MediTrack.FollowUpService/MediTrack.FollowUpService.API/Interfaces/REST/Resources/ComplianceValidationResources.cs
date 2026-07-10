namespace MediTrack.FollowUpService.API.Interfaces.REST.Resources;

/// Recursos del flujo de validación de evidencia en video
/// (MediTrack AI Validator — Prototype: la decisión hoy la toma un humano en una
/// web separada; el contrato queda listo para que, a futuro, un modelo real de
/// visión artificial la reemplace sin cambiar esta forma de datos).

/// Respuesta de `POST /api/v1/compliance/video`.
public record VideoSubmissionResource(int ComplianceId, string Status);

/// Un caso en la bandeja de `GET /api/v1/compliance/pending-validation`.
/// Solo metadatos necesarios para revisar — sin datos personales del paciente
/// más allá de su id numérico.
public record PendingValidationItemResource(
    int ComplianceId,
    int PatientId,
    string MedicationName,
    string Dose,
    DateTime? ScheduledAt,
    DateTime SubmittedAt,
    string VideoEndpoint
);

/// Body opcional de `PATCH /api/v1/compliance/{id}/reject`.
public record RejectComplianceResource(string? RejectionReason);

/// Respuesta de aprobar/rechazar y de `GET /api/v1/compliance/{id}/status`.
public record ComplianceValidationStatusResource(
    int ComplianceId,
    string Status,
    string? RejectionReason,
    DateTime? ValidatedAt
);
