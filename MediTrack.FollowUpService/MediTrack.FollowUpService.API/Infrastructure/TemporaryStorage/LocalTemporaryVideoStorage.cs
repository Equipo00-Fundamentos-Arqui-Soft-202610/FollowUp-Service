using MediTrack.FollowUpService.API.Domain.Model;

namespace MediTrack.FollowUpService.API.Infrastructure.TemporaryStorage;

/// Implementación de disco local para <see cref="ITemporaryVideoStorage"/>.
/// Carpeta privada fuera de wwwroot (nunca se registra `UseStaticFiles` sobre
/// ella), así que no queda expuesta como contenido estático público — el único
/// acceso es a través de `GET /api/v1/compliance/{id}/video`, controlado y
/// disponible solo mientras el caso está `PendingValidation`.
public class LocalTemporaryVideoStorage : ITemporaryVideoStorage
{
    private readonly string _folderPath;
    private readonly ILogger<LocalTemporaryVideoStorage> _logger;

    public LocalTemporaryVideoStorage(ILogger<LocalTemporaryVideoStorage> logger)
    {
        _logger = logger;
        _folderPath = Path.Combine(AppContext.BaseDirectory, "PendingComplianceVideos");
        Directory.CreateDirectory(_folderPath);
    }

    public async Task<string> SaveAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        var fullPath = Path.Combine(_folderPath, fileName);

        await using var output = new FileStream(fullPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(output, cancellationToken);

        return fileName;
    }

    public Stream? OpenRead(string fileName)
    {
        var fullPath = Path.Combine(_folderPath, fileName);
        return File.Exists(fullPath) ? File.OpenRead(fullPath) : null;
    }

    public void Delete(string fileName)
    {
        try
        {
            var fullPath = Path.Combine(_folderPath, fileName);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "No se pudo eliminar el video temporal {FileName}", fileName);
        }
    }
}
