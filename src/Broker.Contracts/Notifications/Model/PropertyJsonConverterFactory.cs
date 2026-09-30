using System.Text.Json;
using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model;

/// <summary>
/// Handles deserialization of <see cref="Property{T}"/> from both NGSI-LD format
/// (e.g. <c>{"type":"Property","value":"..."}</c>) and plain values (e.g. <c>"..."</c>).
/// </summary>
public class PropertyJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType
               && typeToConvert.GetGenericTypeDefinition() == typeof(Property<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(PropertyJsonConverter<>).MakeGenericType(valueType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

public sealed class PropertyJsonConverter<T> : JsonConverter<Property<T>>
{
    public override Property<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var property = new Property<T>();

            if (root.TryGetProperty("type", out var typeElem))
            {
                property.Type = typeElem.GetString();
            }

            if (root.TryGetProperty("value", out var valueElem))
            {
                property.Value = JsonSerializer.Deserialize<T>(valueElem.GetRawText(), options);
            }

            return property;
        }

        // Plain value (string, number, bool, etc.) — wrap it in a Property<T>
        var value = JsonSerializer.Deserialize<T>(ref reader, options);
        return new Property<T>
        {
            Type = "Property",
            Value = value
        };
    }

    public override void Write(Utf8JsonWriter writer, Property<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Type != null)
        {
            writer.WriteString("type", value.Type);
        }

        writer.WritePropertyName("value");
        JsonSerializer.Serialize(writer, value.Value, options);

        writer.WriteEndObject();
    }
}
