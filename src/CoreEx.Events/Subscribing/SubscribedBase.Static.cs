namespace CoreEx.Events.Subscribing;

public abstract partial class SubscribedBase
{
    private static readonly Regex _wildcardAllRegex = AllRegex();
    private static readonly ConcurrentDictionary<(string, char, RegexOptions?), Regex> _regexCache = new();

    /// <summary>
    /// Indicates whether the <see cref="string"/>-based <paramref name="input"/> matches the regular expression.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/>.</param>
    /// <param name="input">The <see cref="string"/> to search for a match.</param>
    /// <returns><see langword="true"/> indicates a match; otherwise, <see langword="false"/>.</returns>
    public static bool IsMatch(Regex regex, string? input) => regex.ThrowIfNull().IsMatch(input ?? string.Empty);

    /// <summary>
    /// Indicates whether the <see cref="Uri"/>-based <paramref name="input"/> matches the regular expression.
    /// </summary>
    /// <param name="regex">The <see cref="Regex"/>.</param>
    /// <param name="input">The <see cref="Uri"/> to search for a match.</param>
    /// <returns><see langword="true"/> indicates a match; otherwise, <see langword="false"/>.</returns>
    public static bool IsUriMatch(Regex regex, Uri? input) => IsMatch(regex, input?.OriginalString);

    /// <summary>
    /// Indicates whether the <paramref name="input"/> matches the segmented (using the <paramref name="separator"/>) glob-like matching pattern.
    /// </summary>
    /// <param name="pattern">The dot-based glob-like matching pattern.</param>
    /// <param name="input">The string to search for a match.</param>
    /// <param name="separator">The segment separator.</param>
    /// <returns><see langword="true"/> indicates a match; otherwise, <see langword="false"/>.</returns>
    /// <remarks>See <see cref="SubscribeAttribute"/> for further details on the dot-based glob-like matching pattern.</remarks>
    public static bool IsMatch(string? pattern, string? input, char separator = '.') => string.IsNullOrEmpty(pattern) || IsMatch(CreateGlobRegex(pattern, separator), input);

    /// <summary>
    /// Indicates whether the <paramref name="input"/> matches the segmented (using the <paramref name="separator"/>) glob-like matching pattern.
    /// </summary>
    /// <param name="pattern">The dot-based glob-like matching pattern.</param>
    /// <param name="input">The <see cref="Uri"/> to search for a match.</param>
    /// <param name="separator">The segment separator.</param>
    /// <returns><see langword="true"/> indicates a match; otherwise, <see langword="false"/>.</returns>
    /// <remarks>See <see cref="SubscribeAttribute"/> for further details on the dot-based glob-like matching pattern.</remarks>
    public static bool IsUriMatch(string? pattern, Uri? input, char separator) => string.IsNullOrEmpty(pattern) || IsMatch(CreateGlobRegex(pattern, separator), input?.OriginalString);

    /// <summary>
    /// Creates a <see cref="Regex"/> from the dot-based glob-like matching pattern.
    /// </summary>
    /// <param name="pattern">The glob-like matching pattern.</param>
    /// <param name="separator">The segment separator.</param>
    /// <param name="options">The optional <see cref="RegexOptions"/>.</param>
    /// <returns>The wildcard <see cref="Regex"/>.</returns>
    /// <remarks>See <see cref="SubscribeAttribute"/> for further details on the dot-based glob-like matching pattern.</remarks>
    public static Regex CreateGlobRegex(string pattern, char separator, RegexOptions? options = null)
    {
        options ??= RegexOptions.IgnoreCase | RegexOptions.Compiled;

        if (string.IsNullOrEmpty(pattern))
            return _wildcardAllRegex;

        return _regexCache.GetOrAdd((pattern, separator, options), _ =>
        {
            var regex = Regex.Escape(pattern)
                .Replace(@"\*\*", ".*")                 // Match any number of segments.
                .Replace(@"\*", $@"[^{separator}]*")    // Match a single segment.
                .Replace(@"\?", $@"[^{separator}]");    // Match a single char in a segment.

            return new Regex($"^{regex}$", options.Value);
        });
    }

    [GeneratedRegex(".*", RegexOptions.Compiled)]
    private static partial Regex AllRegex();

    /// <summary>
    /// Indicates whether the expected <paramref name="majorVersion"/> matches the <paramref name="version"/>.
    /// </summary>
    /// <param name="majorVersion">The expected major version number.</param>
    /// <param name="version">The <see cref="Version"/> to compare <see cref="Version.Major"/> with.</param>
    /// <returns><see langword="true"/> indicates a match; otherwise, <see langword="false"/>.</returns>
    public static bool IsVersionMatch(int? majorVersion, Version? version) => majorVersion is null || (version is not null && version.Major == majorVersion.Value);
}