using System.Text.Json;
using Broker.Contracts.Notifications.Model;

namespace Nemobil.Tests.NgsiLd.NotificationsTests
{
    /// <summary>
    /// Das NeMo.bil-Datenmodell und die App liefern die gewuenschte
    /// Ankunftszeit im Feld <c>targetTime</c>. Frueher las das DTO faelschlich
    /// <c>dropoffTime</c> -> bei einer Ankunftszeit-Anfrage blieb das Feld leer und der
    /// Validator lehnte mangels Zeitfenster ab.
    /// </summary>
    public class TripRequestDtoArrivalFieldTests
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

        [Fact]
        public void TargetTime_WirdAlsAnkunftszeitGelesen()
        {
            const string json = """
                {
                    "id": "urn:ngsi-ld:TripRequest:1",
                    "user": "urn:ngsi-ld:User:1",
                    "targetTime": "2026-06-23T11:30:00Z"
                }
                """;

            var dto = JsonSerializer.Deserialize<TripRequestDto>(json, Options);

            Assert.NotNull(dto);
            Assert.Equal("2026-06-23T11:30:00Z", dto.DropoffTime);
        }

        [Fact]
        public void PickupTime_BleibtUnveraendertLesbar()
        {
            const string json = """
                {
                    "id": "urn:ngsi-ld:TripRequest:1",
                    "user": "urn:ngsi-ld:User:1",
                    "pickupTime": "2026-06-23T11:00:00Z"
                }
                """;

            var dto = JsonSerializer.Deserialize<TripRequestDto>(json, Options);

            Assert.NotNull(dto);
            Assert.Equal("2026-06-23T11:00:00Z", dto.PickupTime);
        }
    }
}
