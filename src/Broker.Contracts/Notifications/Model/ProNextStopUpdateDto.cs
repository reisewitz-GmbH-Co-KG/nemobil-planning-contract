using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// PATCH-Payload fuer die Aktualisierung der naechsten Konvoifahrt eines Pro-Entity im NGSI-LD Broker.
    /// Wird mit PATCH /entities/{id}/attrs/ gesendet. Liefert das naechste Zielsegment
    /// (Location + Ankunft) und die anstehende Reihenfolge der zu koppelnden Cabs.
    /// </summary>
    public class ProNextStopUpdateDto
    {
        [JsonPropertyName("nextStopLocation")]
        public GeoProperty? NextStopLocation { get; set; }

        [JsonPropertyName("nextStopArrival")]
        public Property<string>? NextStopArrival { get; set; }

        [JsonPropertyName("toBeChainedVehicles")]
        public Property<List<string>>? ToBeChainedVehicles { get; set; }
    }
}
