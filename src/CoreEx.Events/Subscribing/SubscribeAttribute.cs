namespace CoreEx.Events.Subscribing;

/// <summary>
/// Defines the pattern matching used by the <see cref="SubscribedManager"/> to uniquely identify the <see cref="SubscribedBase"/> by matching against both the <see cref="EventData.Title"/> and <see cref="EventData.Source"/>.
/// </summary>
/// <param name="title">The <see cref="EventData.Title"/> glob-like matching pattern that will represent the underlying <see cref="Title"/> <see cref="Regex"/>.</param>
/// <param name="source">The <see cref="EventData.Source"/> glob-like matching pattern that will represent the underlying <see cref="Source"/> <see cref="Regex"/>.</param>
/// <remarks>This <see cref="SubscribeAttribute"/> allows multiple; in that at least one of the specified attributes needs to match to be considered a successful match.
/// <para>The <see cref="EventData.Title"/> for example may be implemented using a dot-based segmented format; for example: '<c>segment1.segment2.segment3.segmentn</c>'.</para>
/// <para>A glob-like matching pattern supports '<c>*</c>' (single segment), '<c>**</c>' (multiple segments) and '<c>?</c>' (a single character within a single segment).<para/>
/// The following are matching pattern examples for '<c>coreex.system.product.updated.v1</c>':
/// <code>
/// **.product.** (match)
/// core*.system.product.updated.v1 (match)
/// coreex.**.updated.v1 (match)
/// coreex.*.updated.v1 (no match)
/// </code>
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class SubscribeAttribute(string? title = null, string? source = null) : Attribute
{
    private readonly string? _title = title;
    private readonly string? _source = source;

    /// <summary>
    /// Gets or sets the default <see cref="TitleSeparator"/>.
    /// </summary>
    public static char DefaultTitleSeparator { get; set; } = '.';

    /// <summary>
    /// Gets or sets the default <see cref="SourceSeparator"/>.
    /// </summary>
    public static char DefaultSourceSeparator { get; set; } = '/';

    /// <summary>
    /// Gets or sets the <see cref="Regex"/> matching pattern for the <see cref="EventData.Title"/>.
    /// </summary>
    public Regex? Title { get; set; }

    /// <summary>
    /// Gets or sets the title separator character used to split the <see cref="EventData.Title"/> into segments (where auto-creating the underlying <see cref="Title"/> <see cref="Regex"/>).
    /// </summary>
    public char TitleSeparator { get; set; } = DefaultTitleSeparator;

    /// <summary>
    /// Gets or sets the <see cref="Regex"/> matching pattern for the <see cref="EventData.Source"/> (see <see cref="Uri.OriginalString"/>).
    /// </summary>
    public Regex? Source { get; set; }

    /// <summary>
    /// Gets or sets the source separator character used to split the <see cref="EventData.Source"/> into segments (where auto-creating the underlying <see cref="Source"/> <see cref="Regex"/>).
    /// </summary>
    public char SourceSeparator { get; set; } = DefaultSourceSeparator;

    /// <summary>
    /// Indicates whether the event matches the <see cref="Title"/> and <see cref="Source"/>.
    /// </summary>
    /// <param name="title">The event title (i.e. <see cref="EventData.Title"/>).</param>
    /// <param name="source">The event source <see cref="Uri"/> (i.e. <see cref="EventData.Source"/>.</param>
    /// <returns><see langword="true"/> indicates a successful match; otherwise, <see langword="false"/>.</returns>
    public bool IsMatch(string? title, Uri? source)
        => SubscribedBase.IsMatch(Title ?? SubscribedBase.CreateGlobRegex(_title.ThrowIfNullOrEmpty(nameof(Title)), TitleSeparator), title)
        && ((_source is null && Source is null) || SubscribedBase.IsUriMatch(Source ?? SubscribedBase.CreateGlobRegex(_source!, SourceSeparator), source));
}