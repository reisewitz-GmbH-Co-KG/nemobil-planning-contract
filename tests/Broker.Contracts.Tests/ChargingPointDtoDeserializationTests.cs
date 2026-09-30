using System.Text.Json;
using Broker.Contracts.Notifications.Model;

namespace Nemobil.Tests.NgsiLd
{
    /// <summary>
    /// Verifiziert, dass <see cref="ChargingPointDto"/> NGSI-LD-Compaction fuer
    /// <c>openingHours</c> akzeptiert — also sowohl Einzelobjekt als auch Array.
    /// Nutzt eine <see cref="SingleOrArrayJsonConverterFactory"/>-Registrierung, wie sie
    /// auch die Anbindung zentral verwendet.
    /// </summary>
    public class ChargingPointDtoDeserializationTests
    {
        private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
        {
            Converters = { new SingleOrArrayJsonConverterFactory() },
        };

        [Fact]
        public void OpeningHoursAsSingleObject_DeserializesToListWithOneEntry()
        {
            const string json = """
                {
                    "id": "urn:ngsi-ld:ChargingPoint:1",
                    "openingHours": { "Min": "05:00:00", "Max": "23:00:00", "WeekDays": "All" }
                }
                """;

            var dto = JsonSerializer.Deserialize<ChargingPointDto>(json, Options);

            Assert.NotNull(dto);
            Assert.Single(dto.OpeningHours);
            Assert.Equal("All", dto.OpeningHours[0].WeekDays);
            Assert.Equal(TimeSpan.Parse("05:00:00"), dto.OpeningHours[0].Min);
            Assert.Equal(TimeSpan.Parse("23:00:00"), dto.OpeningHours[0].Max);
        }

        [Fact]
        public void OpeningHoursAsArray_DeserializesAllEntries()
        {
            const string json = """
                {
                    "id": "urn:ngsi-ld:ChargingPoint:1",
                    "openingHours": [
                        { "Min": "05:00:00", "Max": "12:00:00", "WeekDays": "Monday" },
                        { "Min": "13:00:00", "Max": "23:00:00", "WeekDays": "Tuesday" }
                    ]
                }
                """;

            var dto = JsonSerializer.Deserialize<ChargingPointDto>(json, Options);

            Assert.NotNull(dto);
            Assert.Equal(2, dto.OpeningHours.Count);
            Assert.Equal("Monday", dto.OpeningHours[0].WeekDays);
            Assert.Equal("Tuesday", dto.OpeningHours[1].WeekDays);
        }

        [Fact]
        public void OpeningHoursMissing_DeserializesToEmptyList()
        {
            const string json = """
                { "id": "urn:ngsi-ld:ChargingPoint:1" }
                """;

            var dto = JsonSerializer.Deserialize<ChargingPointDto>(json, Options);

            Assert.NotNull(dto);
            Assert.NotNull(dto.OpeningHours);
            Assert.Empty(dto.OpeningHours);
        }

        [Fact]
        public void OpeningHoursNull_DeserializesToEmptyList()
        {
            const string json = """
                { "id": "urn:ngsi-ld:ChargingPoint:1", "openingHours": null }
                """;

            var dto = JsonSerializer.Deserialize<ChargingPointDto>(json, Options);

            Assert.NotNull(dto);
            Assert.NotNull(dto.OpeningHours);
            Assert.Empty(dto.OpeningHours);
        }

        [Fact]
        public void OperatorsAsSingleObject_DeserializesToListWithOneEntry()
        {
            const string json = """
                {
                    "id": "urn:ngsi-ld:ChargingPoint:1",
                    "operators": { "chargingOperatorGuid": "00000000-0000-0000-0000-000000000001", "name": "Acme", "defaultPrice": 0.42 }
                }
                """;

            var dto = JsonSerializer.Deserialize<ChargingPointDto>(json, Options);

            Assert.NotNull(dto);
            Assert.Single(dto.Operators);
            Assert.Equal("Acme", dto.Operators[0].Name);
        }
    }
}
