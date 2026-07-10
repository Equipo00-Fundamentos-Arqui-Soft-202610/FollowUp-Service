namespace MediTrack.FollowUpService.API.Domain.Model;

/// Almacenamiento PRIVADO y TEMPORAL de evidencia en video para el prototipo de
/// validación (MediTrack AI Validator — Prototype). A diferencia de
/// <see cref="IBlobStorageService"/> (Azure Blob, persistente y pensado para URLs
/// públicas), este servicio guarda el archivo en disco local, fuera de wwwroot,
/// y se espera que el archivo se elimine apenas se aprueba/rechaza o expira.
public interface ITemporaryVideoStorage
{
    /// Guarda el stream y devuelve el nombre de archivo generado (no la ruta completa).
    Task<string> SaveAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default);

    /// Abre el archivo para lectura/streaming. Null si no existe.
    Stream? OpenRead(string fileName);

    /// Borra el archivo si existe. No lanza si ya fue eliminado.
    void Delete(string fileName);
}
