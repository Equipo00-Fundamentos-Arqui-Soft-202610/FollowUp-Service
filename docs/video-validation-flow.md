# Flujo de validación de evidencia en video — MediTrack AI Validator (Prototype)

## Qué es esto

Un prototipo/simulador del futuro módulo de inteligencia artificial que
validará que un paciente efectivamente tomó su dosis, a partir de un video
corto. **En esta versión la decisión la toma una persona** (human-in-the-loop
validation prototype) desde una aplicación web separada — no hay ningún
modelo de IA analizando el video. El contrato de entrada/salida (video ->
`PendingValidation` -> `Approved`/`Rejected`) queda diseñado para que, a
futuro, un modelo real de visión artificial reemplace al validador humano sin
cambiar la forma de los datos ni los endpoints.

## Flujo actual (prototipo)

```
Paciente
  -> MediTrack-Mobile (graba video, máx. 30s)
  -> API Gateway (/followup/api/v1/compliance/video)
  -> FollowUp-Service (guarda el video en una carpeta privada temporal,
     crea/actualiza el cumplimiento en estado PendingValidation)
  -> MediTrack AI Validator (bandeja de casos pendientes, vía API Gateway)
  -> un validador humano reproduce el video y decide Aprobar/Rechazar
  -> FollowUp-Service (marca Approved/Rejected, borra el video inmediatamente)
  -> MediTrack-Mobile (Home y Progreso se actualizan al consultar de nuevo)
```

## Flujo futuro

El validador humano de "MediTrack AI Validator" será reemplazado por un
modelo real de visión artificial que consuma el mismo contrato:
- Entrada: el mismo video (o los frames que el modelo necesite) accesible por
  el mismo endpoint controlado `GET /api/v1/compliance/{id}/video`.
- Salida: la misma decisión binaria, hoy expresada como dos acciones REST
  (`PATCH .../approve` / `PATCH .../reject`), conceptualmente equivalente a
  `{ "isValid": true|false }`.

Nada del contrato HTTP necesita cambiar para hacer ese reemplazo — solo quién
(o qué) llama a `approve`/`reject`.

## Estados de `MedicationCompliance.Status`

- `taken` / `skipped`: registro directo histórico (sin evidencia), sin cambios.
- `PendingValidation`: video recién subido, esperando revisión.
- `Approved`: evidencia aprobada — cuenta como dosis cumplida para adherencia
  (`ComplianceStatus.IsTaken` ahora es `Value == "taken" || Value == "approved"`).
- `Rejected`: evidencia rechazada — no cuenta para adherencia. El paciente
  puede volver a grabar mientras siga dentro de la ventana de toma (mismo
  registro se actualiza vía upsert, no se duplica la fila).

## Cumplimientos históricos existentes

Los registros con `taken`/`skipped` creados antes de este cambio **no se
tocan**: las columnas nuevas (`ScheduledAt`, `ValidatedAt`, `ValidatorId`,
`RejectionReason`, `TemporaryVideoPath`, `VideoExpiresAt`) son todas nullable
y quedan en `NULL` para esas filas. El cálculo de adherencia sigue
contándolos exactamente igual que antes (`taken` ya contaba, sigue contando).

## Almacenamiento del video

- Se guarda temporalmente en `PendingComplianceVideos/` (carpeta privada,
  fuera de `wwwroot`, no servida como contenido estático).
- Se elimina inmediatamente al aprobar o rechazar.
- Un `BackgroundService` (`StaleComplianceVideoCleanupService`) revisa cada
  hora si hay casos `PendingValidation` con más de 24 horas sin resolver: les
  borra el video y los transiciona a `Rejected` con un motivo explícito
  ("Video expirado sin validación"), para que ningún caso quede sin
  resolución final.
- Los metadatos del cumplimiento (quién, cuándo, qué decisión) permanecen
  para siempre — solo el archivo de video es efímero.
