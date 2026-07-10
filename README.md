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

⚠️ **Acción requerida**: `AzureBlob:ConnectionString` apuntaba a una cuenta REAL
de Azure Storage cuya clave estaba commiteada en git. Hay que regenerar esa
clave desde el Portal de Azure (no se puede hacer por código) y configurarla con:

```bash
dotnet user-secrets set "AzureBlob:ConnectionString" "<connection string con la clave rotada>" --project MediTrack.FollowUpService.API
```

## Ejecución local

```bash
dotnet run --project MediTrack.FollowUpService.API
```
