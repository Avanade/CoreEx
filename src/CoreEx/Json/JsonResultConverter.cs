namespace CoreEx.Json;

/// <summary>
/// Provides an <see cref="IResult"/> converter to ensure the correct type is used to serialize.
/// </summary>
/// <remarks>This converter only supports serialization and not deserialization as an <see cref="IResult"/> is not a type expected to be exchanged.</remarks>
public class JsonResultConverter : JsonConverter<IResult>
{
    /// <inheritdoc/>
    public override bool CanConvert(Type typeToConvert) => typeToConvert == typeof(Result) || typeToConvert == typeof(Result<>);

    /// <inheritdoc/>
    public override IResult? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotSupportedException($"Deserialization is not supported for an {nameof(IResult)} as this type is not expected to be exchanged.");

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer, IResult value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("IsSuccess");
        writer.WriteBooleanValue(value.IsSuccess);

        if (value.IsSuccess)
        {
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, value.Value, value.Value?.GetType() ?? typeof(object), options);
        }
        else
        {
            writer.WritePropertyName("Error");
            JsonSerializer.Serialize(writer, value.Error, value.Error?.GetType() ?? typeof(Exception), options);
        }

        writer.WriteEndObject();
    }
}