// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Localization;
using System.Resources;

namespace CoreEx.Validation
{
    /// <summary>
    /// Provides the <see cref="ITextProvider"/> for validation <see cref="LText"/> localization.
    /// </summary>
    public class ValidationTextProvider : TextProviderBase
    {
        /// <summary>
        /// Gets the <see cref="System.Resources.ResourceManager"/> that contains the texts for the validation.
        /// </summary>
        public static ResourceManager ResourceManager { get; } = new("CoreEx.Validation.Resources", typeof(ValidationTextProvider).Assembly);

        /// <inheritdoc/>
        protected override string? GetTextForKey(LText key) => key.KeyAndOrText is null ? null : ResourceManager.GetString(key.KeyAndOrText, System.Globalization.CultureInfo.CurrentCulture);
    }
}