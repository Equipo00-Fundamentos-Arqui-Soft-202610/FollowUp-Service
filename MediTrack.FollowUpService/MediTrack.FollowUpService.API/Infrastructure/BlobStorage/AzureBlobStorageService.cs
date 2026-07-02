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
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<AzureBlobStorageService> _logger;

    public AzureBlobStorageService(IOptions<AzureBlobOptions> options,
        ILogger<AzureBlobStorageService> logger)
    {
        _logger = logger;
        var blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(options.Value.ContainerName);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var blobName = $"{Guid.NewGuid()}/{fileName}";
        var blobClient = _containerClient.GetBlobClient(blobName);

        _logger.LogInformation("Uploading video to Azure Blob: {BlobName}", blobName);

        await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
        {
            ContentType = contentType
        });

        _logger.LogInformation("Video uploaded successfully: {Uri}", blobClient.Uri);

        return blobClient.Uri.ToString();
    }
}
