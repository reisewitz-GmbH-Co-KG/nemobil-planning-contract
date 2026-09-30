using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    public sealed record CompleteTripDto(string TripGuid)
        : INotification;
}
