# Fix: Home siempre "Sin dosis pendiente" (next-dose)

## Síntoma reportado

- Medicamentos (Treatment-Service) muestra correctamente la próxima dosis y la alarma local suena.
- Home (`GET /followup/api/v1/medications/next-dose`) siempre devuelve 404 → "Sin dosis pendiente", botón "Tomar dosis" nunca se habilita.

## Causa raíz

FollowUp-Service mantiene su **propia réplica local** de `Medication`/`DoseSchedule` (mismos IDs que Treatment-Service — ver `ValueGeneratedNever()` en `FollowUpDbContext`), poblada **exclusivamente** por el evento RabbitMQ `PrescriptionCreated` que publica Treatment-Service al crear una receta.

Esa publicación (`RabbitMqPublisher.PublishAsync`, en Treatment-Service) es *fire-and-forget*:
- Usa `mandatory: true` (le pide al broker devolver el mensaje si no se puede enrutar a ninguna cola), pero **no implementa ningún handler de `BasicReturn`**, ni publisher-confirms, ni reintentos.
- Si en el momento de publicar la cola de FollowUp-Service (`followup-service.prescription-created`) no estaba declarada/enlazada (por ejemplo, porque FollowUp-Service no estaba corriendo), el mensaje se **pierde para siempre** en el exchange topic `meditrack.events` — sin generar ningún error visible, ni en Treatment-Service (que loguea éxito) ni en FollowUp-Service (que nunca lo recibió).

Resultado: la tabla `medications`/`dose_schedules` de `followup_db` queda vacía para ese paciente, aunque `treatment_db` sí tenga la receta activa. `NextPendingDoseQueryService` hace su trabajo correctamente — busca medicamentos activos localmente, no encuentra ninguno, y devuelve `null` (404) — el bug no está en esa lógica de consulta, está en que los datos nunca llegaron.

Se confirmó adicionalmente que FollowUp-Service no tenía ningún mecanismo alterno de sincronización (sin outbox, sin job de reconciliación, sin reintentos) — RabbitMQ era la única vía, y es inherentemente no-garantizada tal como está implementada hoy en Treatment-Service (repositorio que no podemos modificar).

## Corrección aplicada (mínima, sin tocar Treatment-Service)

Se agregó un **respaldo bajo demanda**: `IMedicationReplicaSyncService.EnsureSyncedAsync(patientId)`, invocado al inicio de `NextPendingDoseQueryService` y `MedicationQueryService`:

1. Si la réplica local ya tiene medicamentos para el paciente, no hace nada (no llama a Treatment-Service en cada request — el camino normal sigue siendo los eventos).
2. Si está vacía, consulta el endpoint público **ya existente** de Treatment-Service (`GET /api/v1/medications?patientId=`, sin autenticación) vía `ITreatmentMedicationsClient`, y sincroniza localmente solo los medicamentos `isActive == true`, usando datos reales (nombre, dosis, horarios, `Medication.Id` real de Treatment).

### Limitación conocida (documentada, no resuelta)

El endpoint de Treatment-Service usado como respaldo (`GET /api/v1/medications`) **no expone el id real de cada `DoseSchedule`** — solo la hora formateada (`"HH:mm"`). Por eso, los horarios sincronizados por este respaldo reciben un id local sintético (`medicationId * 1000 + índice`), no el id real de Treatment-Service. Es un identificador técnico interno de FollowUp-Service (el contenido — medicamento, dosis, hora — sí es el real), pero en teoría podría colisionar con un id real sincronizado más adelante por evento para otro medicamento. Dado el volumen de datos de este proyecto (prototipo), el riesgo es bajo; si se requiere una solución robusta a futuro, la opción correcta es que Treatment-Service exponga el id real del `DoseSchedule` en su endpoint público (cambio en Treatment-Service, fuera de este alcance).

Este respaldo **no reemplaza** el mecanismo de eventos — sigue siendo la vía normal para altas/cambios futuros mientras ambos servicios estén corriendo con RabbitMQ disponible.
