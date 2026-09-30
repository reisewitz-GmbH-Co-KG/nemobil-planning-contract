using System.Text.Json.Serialization;
using Mediator;

namespace Broker.Contracts.Notifications.Model
{
    /// <summary>
    /// NGSI-LD Notification fuer Cab-Fahrzeuge.
    /// </summary>
    public class CabNotificationDto : VehicleNotificationDto, INotification
    {
        public CabNotificationDto()
        {
            Type = "Cab";
        }

        /// <summary>
        /// Vom Broker zurückgespiegelter Schlüssel des erreichten Stops (der zuvor von uns als
        /// stopKey mit der nextStopLocation gesendet wurde). Zusammen mit
        /// <see cref="VehicleNotificationDto.StateADStack"/> = "StopReached" das eindeutige
        /// "Wegpunkt erreicht"-Signal.
        /// </summary>
        [JsonPropertyName("stopKey")]
        public string? StopKey { get; set; }
    }
}
