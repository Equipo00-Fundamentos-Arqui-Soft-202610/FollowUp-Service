namespace MediTrack.FollowUpService.API.Domain.Model;

/// Respaldo de sincronización de la réplica local de Medication/DoseSchedule.
///
/// CAUSA RAÍZ documentada (ver docs/next-dose-sync-fix.md): FollowUp-Service
/// mantiene su propia copia de Medication/DoseSchedule, poblada
/// exclusivamente por el evento RabbitMQ "PrescriptionCreated" publicado por
/// Treatment-Service. Esa publicación es fire-and-forget (usa
/// `mandatory: true` pero Treatment-Service no maneja `BasicReturn` ni usa
/// publisher-confirms/reintentos): si la cola de FollowUp-Service no estaba
/// enlazada en el momento de publicar (p. ej. el servicio no estaba
/// corriendo), el evento se pierde para siempre y Treatment-Service igual
/// registra un log de éxito. El resultado observado: la réplica local queda
/// vacía para ese paciente y `next-dose`/`medications` devuelven 404/[]
/// aunque Treatment-Service (la fuente de verdad) sí tenga el medicamento activo.
///
/// Este servicio NO reemplaza el mecanismo de eventos (que sigue siendo la
/// vía normal y de bajo costo para altas/cambios futuros) — solo actúa como
/// respaldo puntual: si la réplica local está vacía para un paciente, se
/// completa consultando el endpoint público YA EXISTENTE de Treatment-Service
/// (sin modificarlo), usando datos reales, no inventados.
public interface IMedicationReplicaSyncService
{
    /// Si la réplica local ya tiene medicamentos para el paciente, no hace
    /// nada (evita llamar a Treatment-Service en cada request). Si está
    /// vacía, sincroniza desde Treatment-Service.
    Task EnsureSyncedAsync(int patientId);
}
