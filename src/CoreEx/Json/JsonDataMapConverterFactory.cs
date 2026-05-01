namespace CoreEx.Json;

/// <summary>
/// Provides a generic JSON converter factory for <see cref="DataMap{TValue}"/> types that preserves the original casing of the underlying keys.
/// </summary>
public class JsonDataMapConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType)
            return false;

        var genericType = typeToConvert.GetGenericTypeDefinition();
        return genericType == typeof(DataMap<>);
    }

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var valueType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(JsonDataMapConverter<>).MakeGenericType(valueType);
        return (JsonConverter?)Activator.CreateInstance(converterType);
    }

    /// <summary>
    /// Provides the JSON converter for <see cref="DataMap{TValue}"/> types, which handles the serialization and deserialization of the dictionary while preserving the original casing of the keys.
    /// </summary>
    private sealed class JsonDataMapConverter<TValue> : JsonConverter<DataMap<TValue>>
    {
        /// <inheritdoc/>
        public override DataMap<TValue>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dict = JsonSerializer.Deserialize<IDictionary<string, TValue>>(ref reader, options);
            return dict is null ? null : new(dict);
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, DataMap<TValue> value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            writer.WriteStartObject();

            foreach (var kvp in value)
            {
                writer.WritePropertyName(kvp.Key);
                JsonSerializer.Serialize(writer, kvp.Value, options);
            }

            writer.WriteEndObject();
        }
    }
}