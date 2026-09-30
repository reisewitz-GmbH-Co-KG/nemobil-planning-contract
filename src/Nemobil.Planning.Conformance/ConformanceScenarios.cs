using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Nemobil.Planning.Conformance
{
    /// <summary>Lädt die mitgelieferten, synthetischen Prüffälle (<c>Fixtures/*.json</c>).
    /// <para>Alle Zeitangaben der Prüffälle beziehen sich auf einen nominalen Tag
    /// (<see cref="NominalDay"/>). Wer eine Implementierung prüft, die nicht in der Vergangenheit planen
    /// darf, verschiebt die Prüffälle mit <c>day</c> auf einen anderen Tag; die Uhrzeiten bleiben
    /// erhalten.</para></summary>
    public static class ConformanceScenarios
    {
        private const string ResourcePrefix = "Nemobil.Planning.Conformance.Fixtures.";
        private const string TimestampFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

        /// <summary>Nominaler Tag der Prüffälle (UTC).</summary>
        public static readonly DateOnly NominalDay = new(2030, 1, 7);

        /// <summary>Serialisierungs-Einstellungen der Prüffälle (camelCase, Aufzählungen als Text).</summary>
        public static JsonSerializerOptions SerializerOptions { get; } = CreateOptions();

        /// <summary>Kennungen aller mitgelieferten Prüffälle, sortiert.</summary>
        public static IReadOnlyList<string> Ids => LoadRaw().Keys.Order(StringComparer.Ordinal).ToList();

        /// <summary>Alle mitgelieferten Prüffälle, sortiert nach Kennung.</summary>
        /// <param name="day">Tag, auf den die Zeitangaben verschoben werden; ohne Angabe der nominale Tag.</param>
        public static IReadOnlyList<ConformanceScenario> All(DateOnly? day = null) =>
            Ids.Select(id => Get(id, day)).ToList();

        /// <summary>Ein mitgelieferter Prüffall.</summary>
        /// <param name="id">Kennung, z. B. <c>G1-01</c>.</param>
        /// <param name="day">Tag, auf den die Zeitangaben verschoben werden; ohne Angabe der nominale Tag.</param>
        /// <exception cref="KeyNotFoundException">Es gibt keinen Prüffall mit dieser Kennung.</exception>
        public static ConformanceScenario Get(string id, DateOnly? day = null)
        {
            if (!LoadRaw().TryGetValue(id, out var json))
            {
                throw new KeyNotFoundException($"Kein Prüffall mit der Kennung '{id}'.");
            }

            return Parse(json, day);
        }

        /// <summary>Liest einen Prüffall im Format der mitgelieferten Fixtures. Damit lassen sich eigene
        /// Prüffälle im selben Format ergänzen.</summary>
        /// <param name="json">Der Prüffall als JSON.</param>
        /// <param name="day">Tag, auf den die Zeitangaben verschoben werden; ohne Angabe unverändert.</param>
        public static ConformanceScenario Parse(string json, DateOnly? day = null)
        {
            var node = JsonNode.Parse(json) ?? throw new JsonException("Leerer Prüffall.");

            if (day is { } target && target != NominalDay)
            {
                Shift(node, TimeSpan.FromDays(target.DayNumber - NominalDay.DayNumber));
            }

            return node.Deserialize<ConformanceScenario>(SerializerOptions)
                   ?? throw new JsonException("Prüffall konnte nicht gelesen werden.");
        }

        private static void Shift(JsonNode node, TimeSpan offset)
        {
            if (node is JsonObject obj)
            {
                foreach (var key in obj.Select(p => p.Key).ToList())
                {
                    if (obj[key] is JsonValue value && TryShift(value, offset, out var shifted))
                    {
                        obj[key] = shifted;
                    }
                    else if (obj[key] is { } child)
                    {
                        Shift(child, offset);
                    }
                }
            }
            else if (node is JsonArray array)
            {
                for (var i = 0; i < array.Count; i++)
                {
                    if (array[i] is JsonValue value && TryShift(value, offset, out var shifted))
                    {
                        array[i] = shifted;
                    }
                    else if (array[i] is { } child)
                    {
                        Shift(child, offset);
                    }
                }
            }
        }

        private static bool TryShift(JsonValue value, TimeSpan offset, out JsonValue shifted)
        {
            shifted = value;

            if (!value.TryGetValue<string>(out var text)
                || !DateTime.TryParseExact(text, TimestampFormat, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var timestamp))
            {
                return false;
            }

            shifted = JsonValue.Create(timestamp.Add(offset).ToString(TimestampFormat, CultureInfo.InvariantCulture));
            return true;
        }

        private static Dictionary<string, string> LoadRaw()
        {
            var assembly = typeof(ConformanceScenarios).Assembly;
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix, StringComparison.Ordinal)))
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();

                var id = JsonNode.Parse(json)?["id"]?.GetValue<string>()
                         ?? throw new InvalidDataException($"Prüffall '{name}' hat keine Kennung.");

                result.Add(id, json);
            }

            return result;
        }

        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                WriteIndented = false,
            };

            options.Converters.Add(new JsonStringEnumConverter());
            return options;
        }
    }
}
