namespace CoreEx.IO.Json;

/// <summary>Provides System.Text.Json scalar converters and ingestion-specific date and finite-number policies.</summary>
public static class JsonFieldConverters
{
    /// <summary>Gets the standard string converter, preserving empty and whitespace-only content.</summary>
    public static JsonConverter<string> String => Default<string>();

    /// <summary>Gets the standard strict 32-bit integer converter.</summary>
    public static JsonConverter<int> Integer => Default<int>();

    /// <summary>Gets the standard strict 64-bit integer converter.</summary>
    public static JsonConverter<long> Int64 => Default<long>();

    /// <summary>Gets the standard decimal converter without intermediate floating-point conversion.</summary>
    public static JsonConverter<decimal> Decimal => Default<decimal>();

    /// <summary>Gets a number converter that rejects non-finite doubles.</summary>
    public static JsonConverter<double> Double { get; } = new FiniteDoubleConverter();

    /// <summary>Gets the standard Boolean converter.</summary>
    public static JsonConverter<bool> Boolean => Default<bool>();

    /// <summary>Gets the standard Guid converter.</summary>
    public static JsonConverter<Guid> Guid => Default<Guid>();

    /// <summary>Creates an invariant date converter with an optional exact format.</summary>
    /// <param name="format">The exact format, or null for standard invariant parsing.</param>
    /// <returns>The System.Text.Json converter.</returns>
    public static JsonConverter<DateOnly> DateOnly(string? format = null) => new DateConverter<DateOnly>(CsvFieldConverters.DateOnly(format), false, format);

    /// <summary>Creates an invariant offset-free date-time converter with an optional exact format.</summary>
    /// <param name="format">The exact format, or null for standard invariant parsing.</param>
    /// <returns>The System.Text.Json converter.</returns>
    public static JsonConverter<DateTime> DateTime(string? format = null) => new DateConverter<DateTime>(CsvFieldConverters.DateTime(format), true, format);

    /// <summary>Creates an invariant date-time converter requiring and preserving an explicit offset.</summary>
    /// <param name="format">The exact format, or null for standard invariant parsing.</param>
    /// <returns>The System.Text.Json converter.</returns>
    public static JsonConverter<DateTimeOffset> DateTimeOffset(string? format = null) => new DateConverter<DateTimeOffset>(CsvFieldConverters.DateTimeOffset(format), false, format);

    /// <summary>Gets a built-in scalar converter without replacing the serializer's object or collection readers.</summary>
    internal static JsonConverter<TValue> Default<TValue>() => (JsonConverter<TValue>)JsonSerializerOptions.Default.GetConverter(typeof(TValue));

    /// <summary>Applies an existing invariant date policy through the standard scalar-converter extension point.</summary>
    private sealed class DateConverter<TValue>(CsvFieldConverter<TValue?> convert, bool rejectOffset, string? format) : JsonConverter<TValue> where TValue : struct
    {
        private readonly bool _rejectOffset = rejectOffset;
        private readonly string? _format = format;

        /// <inheritdoc/>
        public override TValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.String)
                throw new JsonException("A date must be a JSON string.");

            var text = reader.GetString();
            if (text is null || !convert(text, out var value) || value is null
                || _rejectOffset && (FileDateTime.HasOffset(text) || value is System.DateTime { Kind: not DateTimeKind.Unspecified }))
                throw new JsonException("The date does not match the declared format or offset policy.");

            return value.Value;
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, TValue value, JsonSerializerOptions options) => JsonSerializer.Serialize(writer, value);

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is DateConverter<TValue> other && _format == other._format && _rejectOffset == other._rejectOffset;

        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(_format, _rejectOffset);
    }

    /// <summary>Retains the finite-number contract without replacing standard numeric parsing.</summary>
    private sealed class FiniteDoubleConverter : JsonConverter<double>
    {
        /// <inheritdoc/>
        public override double Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var value = reader.GetDouble();
            return double.IsFinite(value) ? value : throw new JsonException("A finite number is required.");
        }

        /// <inheritdoc/>
        public override void Write(Utf8JsonWriter writer, double value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }
}
