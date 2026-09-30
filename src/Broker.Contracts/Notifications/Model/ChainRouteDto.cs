using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// NGSI-LD DTO for ChainRoute entities.
    /// Represents a convoy route that a Pro vehicle drives, along which Cab vehicles can couple/uncouple.
    /// </summary>
    public sealed class ChainRouteDto : NgsiLdBaseType, INotification
    {
        /// <summary>
        /// Gets or sets the NGSI-LD entity id (e.g. "urn:ngsi-ld:ChainRoute:{guid}").
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the reference to the start ChainingLocation (Relationship).
        /// Value is a URN, e.g. "urn:ngsi-ld:ChainingLocation:{guid}".
        /// </summary>
        [JsonPropertyName("startLocation")]
        public Property<string> StartLocation { get; set; }

        /// <summary>
        /// Gets or sets the reference to the end ChainingLocation (Relationship).
        /// Value is a URN, e.g. "urn:ngsi-ld:ChainingLocation:{guid}".
        /// </summary>
        [JsonPropertyName("endLocation")]
        public Property<string> EndLocation { get; set; }

        /// <summary>
        /// Gets or sets the intermediate chaining locations along the route.
        /// Each entry contains a reference to a ChainingLocation and timing offsets.
        /// </summary>
        [JsonPropertyName("intermediateChainingLocations")]
        public Property<List<IntermediateChainingLocationDto>> IntermediateChainingLocations { get; set; }

        /// <summary>
        /// Gets or sets the total route duration in seconds.
        /// </summary>
        [JsonPropertyName("duration")]
        public Property<int> Duration { get; set; }

        /// <summary>
        /// Gets or sets the total route distance in meters.
        /// </summary>
        [JsonPropertyName("distance")]
        public Property<int> Distance { get; set; }
    }

    /// <summary>
    /// Nested DTO for intermediate chaining locations within a ChainRoute.
    /// </summary>
    public sealed class IntermediateChainingLocationDto
    {
        /// <summary>
        /// Gets or sets the reference to the ChainingLocation (URN).
        /// </summary>
        [JsonPropertyName("chainingLocation")]
        public string ChainingLocation { get; set; }

        /// <summary>
        /// Gets or sets the chaining type at this intermediate location.
        /// Allowed values: "NotAllowed", "Chain", "Unchain", "ChainAndUnchain".
        /// </summary>
        [JsonPropertyName("chainingType")]
        public string ChainingType { get; set; }

        /// <summary>
        /// Gets or sets the time offset from route start to arrival at this location (ISO 8601 duration, e.g. "PT10M30S").
        /// </summary>
        [JsonPropertyName("offsetArrival")]
        public string OffsetArrival { get; set; }

        /// <summary>
        /// Gets or sets the time offset from route start to departure from this location (ISO 8601 duration, e.g. "PT12M").
        /// </summary>
        [JsonPropertyName("offsetDeparture")]
        public string OffsetDeparture { get; set; }
    }
}
