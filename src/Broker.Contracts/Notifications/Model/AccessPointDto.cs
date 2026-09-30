using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// NGSI-LD DTO for AccessPoint notifications from the broker.
    /// </summary>
    public sealed class AccessPointDto : NgsiLdBaseType, INotification
    {
        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [JsonPropertyName("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Location.
        /// </summary>
        [JsonPropertyName("location")]
        public GeoProperty Location { get; set; }

        /// <summary>
        /// Gets or sets the Area polygon.
        /// </summary>
        [JsonPropertyName("area")]
        public List<GeoProperty>? Area { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether barrier free access is available.
        /// </summary>
        [JsonPropertyName("barrierFreeAccess")]
        public bool BarrierFreeAccess { get; set; }

        /// <summary>
        /// Gets or sets the maximum stopping time.
        /// </summary>
        [JsonPropertyName("maxStoppingTime")]
        public int MaxStoppingTime { get; set; }
    }
}
