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

    /// <summary>Creates an invariant nullable date converter using an exact format.</summary>
    /// <param name="format">The required DateOnly format.</param>
    /// <returns>The non-throwing date converter.</returns>
    public static CsvFieldConverter<DateOnly?> DateOnly(string format)
    {
        format.ThrowIfNullOrWhiteSpace();
        return (ReadOnlySpan<char> text, out DateOnly? value) =>
        {
            if (text.IsWhiteSpace())
            {
                value = null;
                return true;
            }

            var success = System.DateOnly.TryParseExact(text, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date);
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
