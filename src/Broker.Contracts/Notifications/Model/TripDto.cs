using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    public class TripDto : NgsiLdBaseType, INotification
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("pickupTime")]
        public Property<string> PickupTime { get; set; }

        [JsonPropertyName("pickupLocation")]
        public GeoProperty PickupLocation { get; set; }

        [JsonPropertyName("dropoffLocation")]
        public GeoProperty DropoffLocation { get; set; }

        [JsonPropertyName("requestedAdults")]
        public Property<int> RequestedAdults { get; set; }

        [JsonPropertyName("user")]
        public Property<string> User { get; set; }

        [JsonPropertyName("status")]
        public Property<string> Status { get; set; }

        [JsonPropertyName("proposal")]
        public Property<string> Proposal { get; set; }
    }
}
