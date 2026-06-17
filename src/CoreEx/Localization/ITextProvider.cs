namespace CoreEx.Localization;

/// <summary>
/// Enables the text localization for an <see cref="LText"/>.
/// </summary>
public interface ITextProvider
{
    /// <summary>
    /// Gets the localized text for the specified <see cref="LText"/>.
    /// </summary>
    /// <param name="text">The <see cref="LText"/>.</param>
    /// <returns>The corresponding text where found; otherwise, the <see cref="LText.FallbackText"/> where specified. Where nothing found or specified then the key itself will be returned.</returns>
    string? GetText(LText text);
}