// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;

namespace CoreEx.Functions
{
    /// <summary>
    /// Provides extensions for <see cref="SettingsBase"/>.
    /// </summary>
    internal static class SettingsBaseExtensions
    {
        /// <summary>
        /// Gets the (default) maximum event publish collection size.
        /// </summary>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <returns>The corresponding value.</returns>
        public static int GetMaxPublishCollSize(this SettingsBase settings) => settings.GetValue("MaxPublishCollSize", 100);
    }
}