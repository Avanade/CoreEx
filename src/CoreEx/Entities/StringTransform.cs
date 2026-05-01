namespace CoreEx.Entities;

/// <summary>
/// Represents a transform option for a <see cref="string"/> value.
/// </summary>
/// <remarks>See <see cref="Cleaner"/>.</remarks>
public enum StringTransform
{
    /// <summary>
    /// Indicates that the <see cref="Cleaner.DefaultStringTransform"/> value should be used.
    /// </summary>
    UseDefault,

    /// <summary>
    /// No transform required; the <see cref="string"/> value will remain as-is.
    /// </summary>
    None,

    /// <summary>
    /// The string will be transformed from a <see langword="null"/> to <see cref="string.Empty"/> value.
    /// </summary>
    NullToEmpty,

    /// <summary>
    /// The string will be transformed from an <see cref="string.Empty"/> value to a <see langword="null"/>.
    /// </summary>
    EmptyToNull
}