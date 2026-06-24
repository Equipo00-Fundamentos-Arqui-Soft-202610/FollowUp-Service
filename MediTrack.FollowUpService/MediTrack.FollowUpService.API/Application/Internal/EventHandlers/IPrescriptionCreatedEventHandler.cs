using MediTrack.FollowUpService.API.Application.OutboundEvents;

namespace MediTrack.FollowUpService.API.Application.Internal.EventHandlers;

public interface IPrescriptionCreatedEventHandler
{
    Task HandleAsync(PrescriptionCreatedEvent evt);
}
