using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using MediTrack.FollowUpService.API.Domain.Model;
using Microsoft.Extensions.Options;

namespace MediTrack.FollowUpService.API.Infrastructure.BlobStorage;

public sealed class R2BlobOptions
{
    public const string SectionName = "R2Blob";
    public string AccountId { get; set; } = string.Empty;
    public string AccessKeyId { get; set; } = string.Empty;
    public string SecretAccessKey { get; set; } = string.Empty;
    public string BucketName { get; set; } = string.Empty;
}

public class R2BlobStorageService : IBlobStorageService
{
    private readonly AmazonS3Client? _client;
    private readonly string _bucketName = string.Empty;
    private readonly ILogger<R2BlobStorageService> _logger;
    private readonly bool _useR2;
    private readonly string _localFallbackPath = string.Empty;

    public R2BlobStorageService(IOptions<R2BlobOptions> options, ILogger<R2BlobStorageService> logger)
    {
        _logger = logger;
        var config = options.Value;
        _useR2 = !string.IsNullOrWhiteSpace(config.AccessKeyId) && !string.IsNullOrWhiteSpace(config.AccountId);

        if (_useR2)
        {
            _bucketName = config.BucketName;
            var credentials = new BasicAWSCredentials(config.AccessKeyId, config.SecretAccessKey);
            _client = new AmazonS3Client(credentials, new AmazonS3Config
            {
                ServiceURL = $"https://{config.AccountId}.r2.cloudflarestorage.com",
                ForcePathStyle = true
            });
            _logger.LogInformation("Cloudflare R2 storage configured. Bucket: {Bucket}", _bucketName);
        }
        else
        {
            _localFallbackPath = Path.Combine(AppContext.BaseDirectory, "blob-storage");
            Directory.CreateDirectory(_localFallbackPath);
            _logger.LogWarning(
                "Cloudflare R2 not configured (AccessKeyId/AccountId empty). " +
                "Falling back to local storage at {Path}.", _localFallbackPath);
        }
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
    {
        var objectKey = $"{Guid.NewGuid()}/{fileName}";

        if (_useR2 && _client != null)
        {
            _logger.LogInformation("Uploading video to Cloudflare R2: {ObjectKey}", objectKey);

            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = objectKey,
                InputStream = fileStream,
                ContentType = contentType
            });

            var url = $"https://{_bucketName}.r2.cloudflarestorage.com/{objectKey}";
            _logger.LogInformation("Video uploaded successfully: {Url}", url);
            return url;
        }

        // Local filesystem fallback
        var localDir = Path.Combine(_localFallbackPath, Path.GetDirectoryName(objectKey) ?? "");
        Directory.CreateDirectory(localDir);

        var localPath = Path.Combine(_localFallbackPath, objectKey.Replace('/', Path.DirectorySeparatorChar));
        await using var fileStream2 = new FileStream(localPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(fileStream2);

        _logger.LogInformation("Video saved locally: {Path}", localPath);
        return localPath;
    }
}
