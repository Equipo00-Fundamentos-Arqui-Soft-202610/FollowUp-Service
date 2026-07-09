using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MediTrack.FollowUpService.API.Domain.Model;
using Microsoft.Extensions.Options;

namespace MediTrack.FollowUpService.API.Infrastructure.BlobStorage;

public sealed class AzureBlobOptions
{
    public const string SectionName = "AzureBlob";
    public string ConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = string.Empty;
}

public class AzureBlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient? _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;
    private readonly bool _useAzure;
    private readonly string _localFallbackPath = string.Empty;

    public AzureBlobStorageService(IOptions<AzureBlobOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        _useAzure = !string.IsNullOrWhiteSpace(options.Value.ConnectionString);

        if (_useAzure)
        {
            var blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
            _logger.LogInformation("Azure Blob Storage configured. Container: {Container}", options.Value.ContainerName);
        }
        else
        {
            _localFallbackPath = Path.Combine(AppContext.BaseDirectory, "blob-storage");
            Directory.CreateDirectory(_localFallbackPath);
            _logger.LogWarning(
                "Azure Blob Storage not configured (ConnectionString is empty). " +
                "Falling back to local storage at {Path}.", _localFallbackPath);
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var blobName = $"{Guid.NewGuid()}/{fileName}";

        if (_useAzure && _containerClient != null)
        {
            var blobClient = _containerClient.GetBlobClient(blobName);
            _logger.LogInformation("Uploading video to Azure Blob: {BlobName}", blobName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
            {
                ContentType = contentType
            });

            _logger.LogInformation("Video uploaded successfully: {Uri}", blobClient.Uri);
            return blobClient.Uri.ToString();
        }

        // Local filesystem fallback
        var localDir = Path.Combine(_localFallbackPath, Path.GetDirectoryName(blobName) ?? "");
        Directory.CreateDirectory(localDir);

        var localPath = Path.Combine(_localFallbackPath, blobName.Replace('/', Path.DirectorySeparatorChar));
        await using var fileStream2 = new FileStream(localPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileStream2);

        _logger.LogInformation("Video saved locally: {Path}", localPath);
        return localPath;
    }
}
