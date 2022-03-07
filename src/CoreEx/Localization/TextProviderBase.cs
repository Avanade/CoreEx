// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Localization
{
    /// <summary>
    /// Provides the localized text for a passed key.
    /// </summary>
    public abstract class TextProviderBase : ITextProvider
    {
        /// <summary>
        /// Gets the text for the passed <see cref="LText"/>.
        /// </summary>
        /// <param name="key">The <see cref="LText"/>.</param>
        /// <returns>The corresponding text where found; otherwise, the <see cref="LText.FallbackText"/> where specified. Where nothing found or specified then the key itself will be returned.</returns>
        public string? GetText(LText key)
        {
            if (key.KeyAndOrText == null)
                return key.FallbackText;

            return GetTextForKey(key) ?? key.FallbackText ?? key.KeyAndOrText;
        }

        /// <summary>
        /// Gets the text for the passed <see cref="LText"/>.
        /// </summary>
        /// <param name="key">The <see cref="LText"/>.</param>
        /// <returns>The corresponding text where found; otherwise, <c>null</c>.</returns>
        protected abstract string? GetTextForKey(LText key);
    }
}