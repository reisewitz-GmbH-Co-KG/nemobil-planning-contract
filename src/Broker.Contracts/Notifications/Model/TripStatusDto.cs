using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    public sealed record TripStatusDto(string TripGuid, string Status)
        : INotification;
}
