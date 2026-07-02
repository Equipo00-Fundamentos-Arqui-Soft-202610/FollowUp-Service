namespace MediTrack.FollowUpService.API.Domain.Model;

public interface IBlobStorageService
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
}
