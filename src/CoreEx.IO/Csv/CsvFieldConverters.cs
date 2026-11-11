namespace CoreEx.IO.Csv;

/// <summary>Provides non-throwing invariant conversions, keeping blank values separate from requiredness.</summary>
public static class CsvFieldConverters
{
    /// <summary>Gets a converter preserving nonblank text and normalizing whitespace-only fields to null.</summary>
    public static CsvFieldConverter<string?> String => TryString;

    /// <summary>Creates an invariant nullable converter; blank fields become null, while malformed nonblank fields fail.</summary>
    /// <typeparam name="TValue">The parsable value type.</typeparam>
    /// <returns>The non-throwing converter.</returns>
    public static CsvFieldConverter<TValue?> Nullable<TValue>() where TValue : struct, ISpanParsable<TValue> => TryNullable<TValue>;

    /// <summary>Creates an invariant nullable date converter with an optional exact format.</summary>
    /// <param name="format">The exact format, or null to use standard invariant parsing.</param>
    /// <returns>The non-throwing date converter.</returns>
    public static CsvFieldConverter<DateOnly?> DateOnly(string? format = null)
    {
        if (format is not null)
            format.ThrowIfNullOrWhiteSpace();

        return (text, out value) =>
        {
            if (text.IsWhiteSpace())
            {
                value = null;
                return true;
            }

            var success = format is null
                ? System.DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                : System.DateOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

            value = success ? date : null;
            return success;
        };
    }

    /// <summary>Creates an invariant nullable DateTime converter with an optional exact format.</summary>
    /// <param name="format">The exact format, or null to use standard invariant parsing.</param>
    /// <returns>The non-throwing converter; blanks become null.</returns>
    public static CsvFieldConverter<DateTime?> DateTime(string? format = null)
    {
        if (format is not null)
            format.ThrowIfNullOrWhiteSpace();

        return (text, out value) =>
        {
            value = null;
            if (text.IsWhiteSpace())
                return true;

            var success = format is null
                ? System.DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                : System.DateTime.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out date);

            value = success ? date : null;
            return success;
        };
    }

    /// <summary>Creates an invariant DateTimeOffset converter requiring an explicit Z or numeric offset.</summary>
    /// <param name="format">The exact format, or null to use standard invariant parsing.</param>
    /// <returns>The non-throwing converter; blanks become null.</returns>
    public static CsvFieldConverter<DateTimeOffset?> DateTimeOffset(string? format = null)
    {
        if (format is not null)
            format.ThrowIfNullOrWhiteSpace();

        return (text, out value) =>
        {
            value = null;
            if (text.IsWhiteSpace())
                return true;

            if (!FileDateTime.HasOffset(text))
                return false;

            var success = format is null
                ? System.DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
                : System.DateTimeOffset.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date);

            value = success ? date : null;
            return success;
        };
    }

    /// <summary>Converts a decoded string field without trimming nonblank text.</summary>
    private static bool TryString(ReadOnlySpan<char> text, out string? value)
    {
        value = text.IsWhiteSpace() ? null : text.ToString();
        return true;
    }

    /// <summary>Converts a nullable value using its span-based invariant parser.</summary>
    private static bool TryNullable<TValue>(ReadOnlySpan<char> text, out TValue? value) where TValue : struct, ISpanParsable<TValue>
    {
        if (text.IsWhiteSpace())
        {
            value = null;
            return true;
        }

        var success = TValue.TryParse(text, CultureInfo.InvariantCulture, out var parsed);
        value = success ? parsed : null;
        return success;
    }
}
