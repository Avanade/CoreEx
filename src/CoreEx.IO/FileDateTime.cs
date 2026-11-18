namespace CoreEx.IO;

/// <summary>Shares the explicit-offset rule between CSV and JSON date conversions.</summary>
internal static partial class FileDateTime
{
    /// <summary>Checks for a trailing UTC designator or numeric offset without assuming a local time zone.</summary>
    /// <param name="value">The source date text.</param>
    /// <returns>Whether an explicit offset is present.</returns>
    public static bool HasOffset(ReadOnlySpan<char> value) => OffsetSuffixRegex().IsMatch(value.TrimEnd());

    /// <summary>Matches an explicit offset without imposing a date grammar.</summary>
    [System.Text.RegularExpressions.GeneratedRegex(@"(?:[zZ]|[+-]\d{2}:\d{2})$")]
    private static partial System.Text.RegularExpressions.Regex OffsetSuffixRegex();
}
