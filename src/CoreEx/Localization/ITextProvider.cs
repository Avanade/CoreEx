// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Localization
{
    /// <summary>
    /// Enables the localized text for a passed key.
    /// </summary>
    public interface ITextProvider
    {
        /// <summary>
        /// Gets the text for the passed <see cref="LText"/>.
        /// </summary>
        /// <param name="key">The <see cref="LText"/>.</param>
        /// <returns>The corresponding text where found; otherwise, the <see cref="LText.FallbackText"/> where specified. Where nothing found or specified then the key itself will be returned.</returns>
        string? GetText(LText key);
    }
}