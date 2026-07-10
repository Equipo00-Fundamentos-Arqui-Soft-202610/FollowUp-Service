using MediTrack.FollowUpService.API.Domain.Model;

namespace MediTrack.FollowUpService.Tests.TestSupport;

/// Doble de prueba en memoria para ITemporaryVideoStorage — evita tocar disco
/// real en los tests y permite verificar que Delete() se invocó.
public class FakeTemporaryVideoStorage : ITemporaryVideoStorage
{
    private readonly Dictionary<string, byte[]> _files = new();
    public List<string> DeletedFileNames { get; } = new();

    public Task<string> SaveAsync(Stream fileStream, string fileExtension, CancellationToken cancellationToken = default)
    {
        var fileName = $"{Guid.NewGuid()}{fileExtension}";
        using var memoryStream = new MemoryStream();
        fileStream.CopyTo(memoryStream);
        _files[fileName] = memoryStream.ToArray();
        return Task.FromResult(fileName);
    }

    public Stream? OpenRead(string fileName)
    {
        return _files.TryGetValue(fileName, out var bytes) ? new MemoryStream(bytes) : null;
    }

    public void Delete(string fileName)
    {
        _files.Remove(fileName);
        DeletedFileNames.Add(fileName);
    }

    public bool Exists(string fileName) => _files.ContainsKey(fileName);
}
