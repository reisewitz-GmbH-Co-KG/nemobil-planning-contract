using System.Text.Json;
using Broker.Contracts.Notifications.Model;

namespace Nemobil.Tests.NgsiLd
{
    public class SingleOrArrayJsonConverterTests
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            Converters = { new SingleOrArrayJsonConverter<OpeningHoursDto>() },
        };

        [Fact]
        public void Read_SingleObject_LiftsIntoListWithOneElement()
        {
            const string json = """
                {"Min":"05:00:00","Max":"23:00:00","WeekDays":"All"}
                """;

            var result = JsonSerializer.Deserialize<List<OpeningHoursDto>>(json, Options);

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(TimeSpan.Parse("05:00:00"), result[0].Min);
            Assert.Equal(TimeSpan.Parse("23:00:00"), result[0].Max);
            Assert.Equal("All", result[0].WeekDays);
        }

        [Fact]
        public void Read_Array_DeserializesAllElements()
        {
            const string json = """
                [
                    {"Min":"05:00:00","Max":"12:00:00","WeekDays":"Monday"},
                    {"Min":"13:00:00","Max":"23:00:00","WeekDays":"Tuesday"}
                ]
                """;

            var result = JsonSerializer.Deserialize<List<OpeningHoursDto>>(json, Options);

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal("Monday", result[0].WeekDays);
            Assert.Equal("Tuesday", result[1].WeekDays);
        }

        [Fact]
        public void Read_EmptyArray_ReturnsEmptyList()
        {
            const string json = "[]";

            var result = JsonSerializer.Deserialize<List<OpeningHoursDto>>(json, Options);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Read_Null_ReturnsEmptyList()
        {
            const string json = "null";

            var result = JsonSerializer.Deserialize<List<OpeningHoursDto>>(json, Options);

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Write_RoundtripsAsArray()
        {
            var list = new List<OpeningHoursDto>
            {
                new OpeningHoursDto { Min = TimeSpan.Parse("05:00:00"), Max = TimeSpan.Parse("23:00:00"), WeekDays = "All" },
            };

            var json = JsonSerializer.Serialize(list, Options);
            var roundTrip = JsonSerializer.Deserialize<List<OpeningHoursDto>>(json, Options);

            Assert.StartsWith("[", json, StringComparison.Ordinal);
            Assert.NotNull(roundTrip);
            Assert.Single(roundTrip);
            Assert.Equal(list[0].WeekDays, roundTrip[0].WeekDays);
        }
    }
}
