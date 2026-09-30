using Mediator;
using Nemobil.Planning.Contracts.Model;

namespace Broker.Contracts.Notifications.Model
{
    public sealed record CabScheduleUpdatedDto(string Guid, GeoPoint CurrentLocation, int EnergyLevelAtStop, DateTime TimeAtStop)
        : INotification;
}
