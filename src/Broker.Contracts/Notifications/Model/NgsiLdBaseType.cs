using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// The ngsi ld base type.
    /// </summary>
    public abstract class NgsiLdBaseType
    {
        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }
    }
}
