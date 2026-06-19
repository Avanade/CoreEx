namespace CoreEx.Json;

/// <summary>
/// Provides an <see cref="IReferenceData"/> converter to ensure the correct type is used to serialize.
/// </summary>
/// <remarks>This converter only supports serialization and not deserialization due to the lack of native polymorphic support for <see cref="IReferenceData"/>.</remarks>
public class JsonReferenceDataConverter : JsonConverter<IReferenceData>
{
    /// <inheritdoc/>
    public override IReferenceData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException($"Deserialization is not supported for an {nameof(IReferenceData)} value as there is no native polymorphic support.");

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IReferenceData value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}