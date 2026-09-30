using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// NGSI-LD Notification fuer Pro-Fahrzeuge (Konvoi-Trgerfahrzeuge).
    /// Erbt die gemeinsamen Vehicle-Felder und ergaenzt Pro-spezifische
    /// Konvoi-Parameter.
    /// </summary>
    public class ProNotificationDto : VehicleNotificationDto, INotification
    {
        public ProNotificationDto()
        {
            Type = "Pro";
        }

        [JsonPropertyName("maxCabChain")]
        public int MaxCabChain { get; set; }

        [JsonPropertyName("maxCabChargingPower")]
        public int MaxCabChargingPower { get; set; } = 11000;

        [JsonPropertyName("additionalEnergyConsumptionPerCab")]
        public float[] AdditionalEnergyConsumptionPerCab { get; set; } = Array.Empty<float>();
    }
}
