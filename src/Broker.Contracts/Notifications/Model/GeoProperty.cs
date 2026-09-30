using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model
{
    public class GeoProperty : NgsiLdBaseType
    {
        [JsonPropertyName("value")]
        public GeoValue Value { get; set; }

        /// <summary>
        /// Gets or sets the coordinates.
        /// </summary>
        [JsonPropertyName("coordinates")]
        public double[] Coordinates { get; set; }
    }
}
