# FollowUp-Service

Microservicio de seguimiento de cumplimiento (medicación, citas, stock) de MediTrack.

## Secretos en desarrollo local

`Jwt:Key` está vacío en `appsettings.json` a propósito -- es compartido con el
Gateway, Identity Service, Reminder-Service y Treatment-service. Cada dev lo
configura una vez en su máquina:

```bash
dotnet user-secrets set "Jwt:Key" "<pedile la clave al equipo>" --project MediTrack.FollowUpService.API
```

En producción esa misma variable se setea como `Jwt__Key` en el entorno del
proveedor de deploy (Render, etc.) -- nunca en un archivo del repo.

**Storage de videos de cumplimiento**: se migró de Azure Blob Storage a
Cloudflare R2 (la AccountKey de Azure había quedado commiteada en git en texto
plano, y R2 tiene free tier permanente sin costo de egreso). Configurar con:

```bash
dotnet user-secrets set "R2Blob:AccountId" "<tu account id de Cloudflare>" --project MediTrack.FollowUpService.API
dotnet user-secrets set "R2Blob:AccessKeyId" "<access key del token R2>" --project MediTrack.FollowUpService.API
dotnet user-secrets set "R2Blob:SecretAccessKey" "<secret key del token R2>" --project MediTrack.FollowUpService.API
```

## Ejecución local

```bash
dotnet run --project MediTrack.FollowUpService.API
```
