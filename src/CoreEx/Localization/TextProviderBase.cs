namespace CoreEx.Localization;

/// <summary>
/// Provides the base text localization functionality for an <see cref="LText"/>.
/// </summary>
public abstract class TextProviderBase : ITextProvider
{
    /// <inheritdoc/>
    public string? GetText(LText text)
    {
        if (text.KeyAndOrText is null)
            return TextProvider.Format(text.FallbackText, text.Args);

        return GetFormattedText(text) ?? TextProvider.Format(text.FallbackText ?? (text.WasFallBackTextSetToNull ? null : text.KeyAndOrText), text.Args);
    }

    /// <summary>
    /// Gets the final formatted localized text for the specified <see cref="LText"/>.
    /// </summary>
    /// <param name="text">The <see cref="LText"/>.</param>
    /// <returns>The corresponding localized text where found; otherwise, <see langword="null"/> (will use the <see cref="LText.FallbackText"/> where specified).</returns>
    /// <remarks>This should leverage the <see cref="TextProvider.Format"/> to ensure intended outcome with any included <see cref="LText.Args"/>.</remarks>
    protected abstract string? GetFormattedText(LText text);
}