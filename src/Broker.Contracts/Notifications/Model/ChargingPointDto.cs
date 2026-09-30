using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// NGSI-LD DTO fuer ChargingPoint-Notifications vom Broker.
    /// Bildet Ladepunkte ab (Smart-Data-Models EVChargingStation-aehnlich).
    /// </summary>
    public sealed class ChargingPointDto : NgsiLdBaseType, INotification
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("location")]
        public GeoProperty Location { get; set; }

        [JsonPropertyName("owner")]
        public Guid Owner { get; set; }

        /// <summary>
        /// Status der Ladesaeule. Erwartete Werte: "Free", "InUse", "Maintenance".
        /// </summary>
        [JsonPropertyName("state")]
        public string State { get; set; }

        [JsonPropertyName("voltage")]
        public float Voltage { get; set; }

        [JsonPropertyName("amperage")]
        public float Amperage { get; set; }

        [JsonPropertyName("operators")]
        public List<ChargingOperatorDto> Operators { get; set; } = new List<ChargingOperatorDto>();

        /// <summary>
        /// Typ der Ladesaeule. Erwartete Werte: "Electric", "Hydrogen", "AC", "DC".
        /// </summary>
        [JsonPropertyName("chargingType")]
        public string ChargingType { get; set; }

        /// <summary>
        /// Oeffnungszeiten der Ladestation. NGSI-LD-Compaction kann ein einzelnes Objekt
        /// statt eines Arrays liefern; die zentral registrierte
        /// <see cref="SingleOrArrayJsonConverterFactory"/> hebt das Einzelobjekt in eine
        /// Liste mit einem Element.
        /// </summary>
        [JsonPropertyName("openingHours")]
        public List<OpeningHoursDto> OpeningHours { get; set; } = new List<OpeningHoursDto>();

        /// <summary>
        /// Maximale Leistung der Ladesaeule (in W).
        /// </summary>
        [JsonPropertyName("maximumPowerSupply")]
        public float MaximumPowerSupply { get; set; }

        /// <summary>
        /// Maximale Standzeit in Sekunden.
        /// </summary>
        [JsonPropertyName("maximumStoppingTime")]
        public float MaximumStoppingTime { get; set; }

        /// <summary>
        /// Maximale Rueckspeiseleistung von Fahrzeugen.
        /// </summary>
        [JsonPropertyName("maximumRegenerativePower")]
        public float MaximumRegenerativePower { get; set; }
    }

    /// <summary>
    /// Betreiber-Eintrag eines Ladepunkts.
    /// </summary>
    public sealed class ChargingOperatorDto
    {
        [JsonPropertyName("chargingOperatorGuid")]
        public Guid ChargingOperatorGuid { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("defaultPrice")]
        public float DefaultPrice { get; set; }
    }

    /// <summary>
    /// Oeffnungszeit-Eintrag im Broker-Payload (Min/Max/WeekDays).
    /// </summary>
    public sealed class OpeningHoursDto
    {
        [JsonPropertyName("Min")]
        public TimeSpan Min { get; set; }

        [JsonPropertyName("Max")]
        public TimeSpan Max { get; set; }

        /// <summary>
        /// Wochentage als Flag-String (z. B. "All", "Monday", "Monday, Tuesday").
        /// </summary>
        [JsonPropertyName("WeekDays")]
        public string WeekDays { get; set; }
    }
}
