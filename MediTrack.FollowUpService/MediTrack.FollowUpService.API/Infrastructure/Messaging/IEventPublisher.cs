namespace MediTrack.FollowUpService.API.Infrastructure.Messaging;

public interface IEventPublisher
{
    Task PublishAsync(string routingKey, object payload);
}
