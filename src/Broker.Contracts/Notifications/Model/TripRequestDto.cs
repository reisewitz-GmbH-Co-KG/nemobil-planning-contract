using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    public class TripRequestDto
        : NgsiLdBaseType, INotification
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("user")]
        public string User { get; set; }

        [JsonPropertyName("pickupTime")]
        public string PickupTime { get; set; }

        // Gewuenschte Ankunftszeit. Das NeMo.bil-Datenmodell und die App
        // liefern sie im Feld "targetTime" (nicht "dropoffTime"); intern bleibt es die
        // DropoffTime.
        [JsonPropertyName("targetTime")]
        public string DropoffTime { get; set; }

        [JsonPropertyName("requestedAdults")]
        public int RequestedAdults { get; set; }

        [JsonPropertyName("requestedChilds")]
        public int RequestedChilds { get; set; }

        [JsonPropertyName("startLocation")]
        public GeoProperty StartLocation { get; set; }

        [JsonPropertyName("targetLocation")]
        public GeoProperty TargetLocation { get; set; }

        [JsonPropertyName("personalPreferences")]
        public PersonalPreferences PersonalPreferences { get; set; }
    }

    public class PersonalPreferences
    {
        [JsonPropertyName("allowCarpooling")]
        public Property<bool> AllowCarpooling { get; set; }

        [JsonPropertyName("toleratedDelayBefore")]
        public Property<int> ToleratedDelayBefore { get; set; }

        [JsonPropertyName("toleratedDelayAfter")]
        public Property<int> ToleratedDelayAfter { get; set; }
    }
}
