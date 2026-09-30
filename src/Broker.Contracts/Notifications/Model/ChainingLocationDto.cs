using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// NGSI-LD DTO for ChainingLocation entities.
    /// Represents a zone where coupling/uncoupling of Cab vehicles to/from a Pro vehicle can occur.
    /// </summary>
    public sealed class ChainingLocationDto : NgsiLdBaseType, INotification
    {
        /// <summary>
        /// Gets or sets the NGSI-LD entity id (e.g. "urn:ngsi-ld:ChainingLocation:{guid}").
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the start of the coupling zone.
        /// </summary>
        [JsonPropertyName("locationStart")]
        public GeoProperty LocationStart { get; set; }

        /// <summary>
        /// Gets or sets the end of the coupling zone.
        /// Coupling is intended for straight road segments, so the zone spans from start to end.
        /// </summary>
        [JsonPropertyName("locationEnd")]
        public GeoProperty LocationEnd { get; set; }

        /// <summary>
        /// Gets or sets the chaining type.
        /// Allowed values: "NotAllowed", "Chain", "Unchain", "ChainAndUnchain".
        /// </summary>
        [JsonPropertyName("typeChainLocation")]
        public Property<string> TypeChainLocation { get; set; }

        /// <summary>
        /// Gets or sets the additional time required for coupling/uncoupling at this location, in seconds.
        /// </summary>
        [JsonPropertyName("additionalTime")]
        public Property<int> AdditionalTime { get; set; }
    }
}
