namespace CoreEx.Localization;

/// <summary>
/// Provides a <see langword="null"/> <see cref="TextProviderBase"/> implementation; the <see cref="GetFormattedText"/> will always return <see langword="null"/>.
/// </summary>
public class NullTextProvider : TextProviderBase
{
    /// <inheritdoc/>
    protected override string? GetFormattedText(LText text) => null;
}