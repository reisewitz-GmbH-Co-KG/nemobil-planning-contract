using System.Text.Json;
using System.Text.Json.Serialization;

namespace Broker.Contracts.Notifications.Model;

/// <summary>
/// Factory, die <see cref="SingleOrArrayJsonConverter{T}"/> fuer jede <see cref="List{T}"/>-Property
/// erzeugt. Zentral in den Broker-Deserialisierungs-Optionen registriert, damit alle eingehenden
/// NGSI-LD-Payloads die Compaction-Form (Einzelobjekt statt Array) tolerieren.
/// </summary>
public sealed class SingleOrArrayJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert.IsGenericType
               && typeToConvert.GetGenericTypeDefinition() == typeof(List<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var elementType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(SingleOrArrayJsonConverter<>).MakeGenericType(elementType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

/// <summary>
/// Liest eine JSON-Property entweder als einzelnes Objekt oder als Array in eine <see cref="List{T}"/>.
/// Hintergrund: NGSI-LD-Compaction liefert Arrays mit nur einem Element als blankes Objekt
/// (ohne Array-Wrapper). Damit Empfaenger beide Formen verarbeiten koennen, hebt dieser
/// Converter ein Einzelobjekt in eine Liste mit einem Element.
/// </summary>
public sealed class SingleOrArrayJsonConverter<T> : JsonConverter<List<T>>
{
    public override bool HandleNull => true;

    public override List<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return new List<T>();
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            var list = new List<T>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndArray)
                {
                    return list;
                }

                var item = JsonSerializer.Deserialize<T>(ref reader, options);
                if (item is not null)
                {
                    list.Add(item);
                }
            }

            throw new JsonException("Unerwartetes JSON-Ende beim Lesen eines SingleOrArray-Wertes.");
        }

        var single = JsonSerializer.Deserialize<T>(ref reader, options);
        return single is null ? new List<T>() : new List<T> { single };
    }

    public override void Write(Utf8JsonWriter writer, List<T> value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            JsonSerializer.Serialize(writer, item, options);
        }

        writer.WriteEndArray();
    }
}
