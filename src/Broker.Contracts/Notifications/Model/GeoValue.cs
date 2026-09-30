using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// The geo value.
    /// </summary>
    public class GeoValue : NgsiLdBaseType
    {
        /// <summary>
        /// Gets or sets the coordinates.
        /// </summary>
        [JsonPropertyName("coordinates")]
        public double[] Coordinates { get; set; }
    }
}
