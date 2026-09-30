using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    public sealed record BookTripDto(string UserGuid, string ProposalGuid, string TransactionGuid, string Source = "Broker")
        : INotification;
}
