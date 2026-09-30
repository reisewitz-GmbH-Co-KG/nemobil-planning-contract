using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// PATCH-Payload für die Aktualisierung der nächsten Haltestelle eines Vehicle-Entity im NGSI-LD Broker.
    /// Wird mit PATCH /entities/{id}/attrs/ gesendet.
    /// </summary>
    public class VehicleNextStopUpdateDto
    {
        [JsonPropertyName("nextStopLocation")]
        public GeoProperty? NextStopLocation { get; set; }

        [JsonPropertyName("nextStopArrival")]
        public Property<string>? NextStopArrival { get; set; }

        /// <summary>
        /// Eindeutiger Schlüssel des nächsten Stops. Der Broker spiegelt ihn bei
        /// "Wegpunkt erreicht" (stateADStack = StopReached) zurück, damit wir den
        /// betroffenen Stop ohne Koordinaten-Abgleich identifizieren können.
        /// </summary>
        [JsonPropertyName("stopKey")]
        public Property<string>? StopKey { get; set; }
    }
}
