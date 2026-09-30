using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    public sealed record StartTripDto(string TripGuid)
        : INotification;
}
