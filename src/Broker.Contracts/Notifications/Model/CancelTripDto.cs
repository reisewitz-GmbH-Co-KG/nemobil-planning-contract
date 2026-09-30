using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    public sealed record CancelTripDto(string TripGuid) : INotification;
}
