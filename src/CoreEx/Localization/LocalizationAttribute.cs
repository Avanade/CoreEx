namespace CoreEx.Localization;

/// <summary>
/// An attribute to define the localization <see cref="LText"/> for a property.
/// </summary>
/// <param name="keyAndOrText">The key and/or text.</param>
/// <param name="fallbackText">The optional fallback text to be used when the <paramref name="keyAndOrText"/> is not found.</param>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public class LocalizationAttribute(string keyAndOrText, string? fallbackText = null) : Attribute
{
    /// <summary>
    /// Gets the key and/or text (where the key is not found, it will used as the text; unless a <see cref="FallbackText"/> is specified.
    /// </summary>
    public string? KeyAndOrText { get; } = keyAndOrText.ThrowIfNullOrEmpty();

    /// <summary>
    /// Gets the optional fallback text to be used when the <see cref="KeyAndOrText"/> is not found; where not specified the <see cref="KeyAndOrText"/> becomes the fallback text.
    /// </summary>
    public string? FallbackText { get; } = fallbackText;

    /// <summary>
    /// Creates a new <see cref="LText"/> instance based on the attribute values and any optional arguments.
    /// </summary>
    /// <returns>The new <see cref="LText"/> instance.</returns>
    public LText ToLText() => FallbackText is null ? new LText(KeyAndOrText) : new LText(KeyAndOrText, FallbackText);
}