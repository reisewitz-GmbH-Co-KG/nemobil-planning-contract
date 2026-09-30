using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    public class TripProposalDto
        : NgsiLdBaseType, INotification
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("request")]
        public Property<string> Request { get; set; }

        [JsonPropertyName("pickupTime")]
        public Property<string> PickupTime { get; set; }

        [JsonPropertyName("cabPickupLocation")]
        public GeoProperty cabPickupLocation { get; set; }

        [JsonPropertyName("cabDropoffLocation")]
        public GeoProperty cabDropoffLocation { get; set; }

        [JsonPropertyName("proposalReleaseTime")]
        public Property<string> ProposalReleaseTime { get; set; }
    }
}
