// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Localization
{
    /// <summary>
    /// Provides a null <see cref="TextProviderBase"/> implementation; the <see cref="GetTextForKey"/> will return <c>null</c>.
    /// </summary>
    public class NullTextProvider : TextProviderBase
    {
        /// <summary>
        /// Gets the text for the passed <see cref="LText"/>.
        /// </summary>
        /// <param name="key">The <see cref="LText"/>.</param>
        /// <returns>The corresponding text where found; otherwise <c>null</c>.</returns>
        protected override string? GetTextForKey(LText key) => null;
    }
}