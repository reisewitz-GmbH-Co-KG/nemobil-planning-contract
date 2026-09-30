using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// NGSI-LD DTO for ChainRouteSchedule entities.
    /// Represents a concrete scheduled convoy journey - a Pro vehicle driving a ChainRoute
    /// at a specific departure/arrival time. Start and end locations and the distance are
    /// taken from the referenced ChainRoute.
    /// </summary>
    public sealed class ChainRouteScheduleDto : NgsiLdBaseType, INotification
    {
        /// <summary>
        /// Gets or sets the NGSI-LD entity id (e.g. "urn:ngsi-ld:ChainRouteSchedule:{guid}").
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the reference to the ChainRoute being driven (Relationship).
        /// Value is a URN, e.g. "urn:ngsi-ld:ChainRoute:{guid}".
        /// </summary>
        [JsonPropertyName("chainRoute")]
        public Property<string> ChainRoute { get; set; }

        /// <summary>
        /// Gets or sets the reference to the Pro schedule executing this trip (Relationship).
        /// Value is a URN, e.g. "urn:ngsi-ld:Pro:{guid}".
        /// </summary>
        [JsonPropertyName("proSchedule")]
        public Property<string> ProSchedule { get; set; }

        /// <summary>
        /// Gets or sets the departure time from the start location (ISO 8601).
        /// </summary>
        [JsonPropertyName("departure")]
        public Property<string> Departure { get; set; }

        /// <summary>
        /// Gets or sets the arrival time at the end location (ISO 8601).
        /// </summary>
        [JsonPropertyName("arrival")]
        public Property<string> Arrival { get; set; }

        /// <summary>
        /// Gets or sets the optional schedule duration in seconds.
        /// If null, the duration is taken from the referenced ChainRoute.
        /// </summary>
        [JsonPropertyName("duration")]
        public Property<int?> Duration { get; set; }
    }
}
