using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model
{
    [JsonConverter(typeof(PropertyJsonConverterFactory))]
    public class Property<T> : NgsiLdBaseType
    {
        [JsonPropertyName("value")]
        public T Value { get; set; }
    }
}
