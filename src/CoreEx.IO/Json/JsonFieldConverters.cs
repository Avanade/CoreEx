namespace CoreEx.IO.Json;

/// <summary>Provides strict native-token conversions without string or floating-point coercion.</summary>
public static class JsonFieldConverters
{
    /// <summary>Converts a string, preserving empty and whitespace-only content.</summary>
    /// <param name="element">The JSON token to convert.</param>
    /// <param name="value">The decoded string, or an empty string when conversion fails.</param>
    /// <returns>Whether the token is a JSON string.</returns>
    public static bool String(JsonElement element, out string value)
    {
        value = string.Empty;
        if (element.ValueKind != JsonValueKind.String)
            return false;

        try
        {
            value = element.GetString() ?? throw new InvalidOperationException("A string token cannot contain null.");
            return true;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    /// <summary>Converts an exact 32-bit integer number token.</summary>
    /// <param name="element">The JSON token to convert.</param>
    /// <param name="value">The converted integer, or its default value when conversion fails.</param>
    /// <returns>Whether the token is a JSON number representable as a 32-bit integer.</returns>
    public static bool Integer(JsonElement element, out int value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value);
    }

    /// <summary>Converts an exact 64-bit integer number token.</summary>
    /// <param name="element">The JSON token to convert.</param>
    /// <param name="value">The converted integer, or its default value when conversion fails.</param>
    /// <returns>Whether the token is a JSON number representable as a 64-bit integer.</returns>
    public static bool Int64(JsonElement element, out long value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Number && element.TryGetInt64(out value);
    }

    /// <summary>Converts a decimal number without an intermediate Double.</summary>
    /// <param name="element">The JSON token to convert.</param>
    /// <param name="value">The converted decimal, or its default value when conversion fails.</param>
    /// <returns>Whether the token is a JSON number representable as a decimal.</returns>
    public static bool Decimal(JsonElement element, out decimal value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Number && element.TryGetDecimal(out value);
    }

    /// <summary>Converts a finite double number token.</summary>
    /// <param name="element">The JSON token to convert.</param>
    /// <param name="value">The converted finite double, or its default value when conversion fails.</param>
    /// <returns>Whether the token is a finite JSON number representable as a double.</returns>
    public static bool Double(JsonElement element, out double value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Number && element.TryGetDouble(out value) && double.IsFinite(value);
    }

    /// <summary>Converts a Boolean token, not a string or number.</summary>
    /// <param name="element">The JSON token to convert.</param>
    /// <param name="value">The converted Boolean value.</param>
    /// <returns>Whether the token is a JSON Boolean.</returns>
    public static bool Boolean(JsonElement element, out bool value)
    {
        value = element.ValueKind == JsonValueKind.True;
        return element.ValueKind is JsonValueKind.True or JsonValueKind.False;
    }

    /// <summary>Converts a Guid string.</summary>
    /// <param name="element">The JSON token to convert.</param>
    /// <param name="value">The parsed identifier, or its default value when conversion fails.</param>
    /// <returns>Whether the token is a string containing a valid identifier.</returns>
    public static bool Guid(JsonElement element, out Guid value)
    {
        value = default;
        return String(element, out var text) && System.Guid.TryParse(text, out value);
    }

    /// <summary>Creates an invariant DateOnly converter with an optional exact format.</summary>
    /// <param name="format">The exact format, or null to use standard invariant parsing.</param>
    /// <returns>A converter for JSON string tokens containing a valid date.</returns>
    public static JsonValueConverter<DateOnly> DateOnly(string? format = null) => (element, out value) =>
    {
        value = default;
        return String(element, out var text) && (format is null
            ? System.DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
            : System.DateOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value));
    };

    /// <summary>Creates an invariant offset-free DateTime converter with an optional exact format.</summary>
    /// <param name="format">The exact format, or null to use standard invariant parsing.</param>
    /// <returns>A converter for JSON string tokens without an offset.</returns>
    public static JsonValueConverter<DateTime> DateTime(string? format = null) => (element, out value) =>
    {
        value = default;
        if (!String(element, out var text) || FileDateTime.HasOffset(text))
            return false;

        return (format is null
            ? System.DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
            : System.DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            && value.Kind == DateTimeKind.Unspecified;
    };

    /// <summary>Creates an invariant DateTimeOffset converter preserving a required Z or numeric offset.</summary>
    /// <param name="format">The exact format, or null to use standard invariant parsing.</param>
    /// <returns>A converter for JSON string tokens matching the format and containing an explicit offset.</returns>
    public static JsonValueConverter<DateTimeOffset> DateTimeOffset(string? format = null) => (element, out value) =>
    {
        value = default;
        if (!String(element, out var text) || !FileDateTime.HasOffset(text))
            return false;

        return format is null
            ? System.DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out value)
            : System.DateTimeOffset.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out value);
    };
}
