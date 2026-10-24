namespace CoreEx.Json;

/// <summary>
/// Creates JSON converters for <see cref="Optional{T}"/> values.
/// </summary>
public sealed class OptionalJsonConverterFactory : JsonConverterFactory
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(Optional<>);

    /// <inheritdoc/>
    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        => (JsonConverter)Activator.CreateInstance(typeof(OptionalJsonConverter<>).MakeGenericType(typeToConvert.GetGenericArguments()[0]))!;

    /// <summary>
    /// Converts an optional value using its wrapped value's JSON representation.
    /// </summary>
    /// <typeparam name="T">The wrapped value type.</typeparam>
    private sealed class OptionalJsonConverter<T> : JsonConverter<Optional<T>>
    {
        /// <inheritdoc/>
        public override bool HandleNull => true;

        /// <inheritdoc/>
        public override Optional<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => Optional<T>.Specified(JsonSerializer.Deserialize<T>(ref reader, options));

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, Optional<T> value, JsonSerializerOptions options)
        {
            if (!value.IsSpecified)
                throw new JsonException("An unspecified Optional value cannot be serialized as a standalone value.");

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }
}
