using System.Text.Json.Serialization;
using Broker.Contracts.Notifications.Model;

namespace Broker.Contracts.Subscriptions
{
    public class Subscription : NgsiLdBaseType
    {
        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("entities")]
        public List<EntitySelector> Entities { get; set; }

        [JsonPropertyName("watchedAttributes")]
        public List<string> WatchedAttributes { get; set; }

        [JsonPropertyName("q")]
        public string Query { get; set; }

        [JsonPropertyName("notification")]
        public NotificationSub Notification { get; set; }

        [JsonPropertyName("@context")]
        public object Context { get; set; }  // string or array of strings
    }

    public class EntitySelector : NgsiLdBaseType;

    public class NotificationSub
    {
        [JsonPropertyName("attributes")]
        public List<string> Attributes { get; set; }

        [JsonPropertyName("format")]
        public string Format { get; set; }

        [JsonPropertyName("endpoint")]
        public Endpoint Endpoint { get; set; }
    }

    public class Endpoint
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; }

        [JsonPropertyName("accept")]
        public string Accept { get; set; }
    }
}
