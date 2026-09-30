using System.Text.Json;
using Broker.Contracts.Notifications.Model;

namespace Nemobil.Tests.NgsiLd.NotificationsTests
{
    /// <summary>Hält die JSON-Form der DTOs fest, die früher von einer Basisklasse aus Demo-Code erbten.
    /// Der Wechsel auf <see cref="NgsiLdBaseType"/> darf an der Serialisierung nichts ändern.</summary>
    public class NgsiLdDtoSerializationCharacterizationTests
    {
        private static readonly JsonSerializerOptions Options = new();

        [Fact]
        public void TripRequestDto_BehaeltSeineJsonForm()
        {
            var dto = JsonSerializer.Deserialize<TripRequestDto>(
                """{"id":"urn:ngsi-ld:TripRequest:1","type":"TripRequest","user":"u1","pickupTime":"2030-01-07T08:00:00Z","requestedAdults":2}""",
                Options)!;

            Assert.Equal("TripRequest", dto.Type);
            Assert.Equal("""{"id":"urn:ngsi-ld:TripRequest:1","user":"u1","pickupTime":"2030-01-07T08:00:00Z","targetTime":null,"requestedAdults":2,"requestedChilds":0,"startLocation":null,"targetLocation":null,"personalPreferences":null,"type":"TripRequest"}""", JsonSerializer.Serialize(dto, Options));
        }

        [Fact]
        public void TripProposalDto_BehaeltSeineJsonForm()
        {
            var dto = new TripProposalDto { Id = "p1", Type = "TripProposal" };

            Assert.Equal("""{"id":"p1","request":null,"pickupTime":null,"cabPickupLocation":null,"cabDropoffLocation":null,"proposalReleaseTime":null,"type":"TripProposal"}""", JsonSerializer.Serialize(dto, Options));
        }

        [Fact]
        public void CabNotificationDto_TraegtWeiterSeinenTyp()
        {
            var json = JsonSerializer.Serialize(new CabNotificationDto { Id = "c1" }, Options);
            var back = JsonSerializer.Deserialize<CabNotificationDto>(json, Options)!;

            Assert.Contains("\"type\":\"Cab\"", json, StringComparison.Ordinal);
            Assert.Equal("Cab", back.Type);
        }
    }
}
