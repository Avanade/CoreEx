namespace CoreEx.Entities;

/// <summary>
/// Represents a transform option for a <see cref="DateTime"/> value.
/// </summary>
/// <remarks><para><i>Note:</i> consider using a <see cref="System.DateOnly"/> or <see cref="DateTimeOffset"/> type as these may be more applicable.</para>See <see cref="Cleaner"/>.</remarks>
public enum DateTimeTransform
{
    /// <summary>
    /// Indicates that the <see cref="Cleaner.DefaultDateTimeTransform"/> value should be used.
    /// </summary>
    UseDefault,

    /// <summary>
    /// No transform required; the <see cref="DateTime"/> value will be left as-is.
    /// </summary>
    None,

    /// <summary>
    /// A <b>DateOnly</b> transform is required; the <see cref="DateTime"/> value will be updated with the <see cref="DateTime.Date"/> only and the <see cref="DateTime.Kind"/> will be set to <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    DateOnly,

    /// <summary>
    /// A <b>DateTime</b> transform is required; the <see cref="DateTime"/> value will be updated as-is and the <see cref="DateTime.Kind"/> will be set to <see cref="DateTimeKind.Local"/>.
    /// </summary>
    DateTimeLocal,

    /// <summary>
    /// A <b>DateTime</b> transform is required; the <see cref="DateTime"/> value will be updated as-is and the <see cref="DateTime.Kind"/> will be set to <see cref="DateTimeKind.Utc"/>.
    /// </summary>
    DateTimeUtc,

    /// <summary>
    /// A <b>DateTime</b> transform is required; the <see cref="DateTime"/> value will be updated as-is and the <see cref="DateTime.Kind"/> will be set to <see cref="DateTimeKind.Unspecified"/>.
    /// </summary>
    DateTimeUnspecified
}