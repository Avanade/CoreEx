namespace CoreEx.Json;

/// <summary>
/// Provides a generic JSON converter factory for <see cref="Exception"/> types.
/// </summary>
public class JsonExceptionConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeof(Exception).IsAssignableFrom(typeToConvert);

    /// <inheritdoc/>
    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(JsonExceptionConverter<>).MakeGenericType(typeToConvert))!;

    /// <summary>
    /// Provides a reflection-based JSON converter for <see cref="Exception"/> types.
    /// </summary>
    /// <typeparam name="TException">The <see cref="Exception"/> <see cref="Type"/>.</typeparam>
    private sealed class JsonExceptionConverter<TException> : JsonConverter<TException> where TException : Exception
    {
        /// <inheritdoc/>
        public override TException? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException();

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TException value, JsonSerializerOptions options)
        {
            var serializableProperties = value.GetType().GetProperties()
                .Select(uu => new 
                { 
                    uu.Name,
                    Value = uu.GetValue(value),
                    Ignore = uu.GetCustomAttribute<JsonIgnoreAttribute>(),
                    JsonName = uu.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name 
                })
                .Where(uu => uu.Ignore is not null && uu.Name != nameof(Exception.TargetSite));

            if (options?.DefaultIgnoreCondition == JsonIgnoreCondition.WhenWritingNull)
                serializableProperties = serializableProperties.Where(uu => uu.Value is not null);

            if (serializableProperties.Any())
            {
                writer.WriteStartObject();
                foreach (var prop in serializableProperties)
                {
                    writer.WritePropertyName(prop.JsonName ?? options?.PropertyNamingPolicy?.ConvertName(prop.Name) ?? prop.Name);
                    JsonSerializer.Serialize(writer, prop.Value, options);
                }

                writer.WriteEndObject();
            }
        }
    }
}