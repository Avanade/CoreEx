﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Globalization;

namespace CoreEx.Globalization
{
    /// <summary>
    /// Provides <see cref="TextInfo"/> extension methods.
    /// </summary>
    public static class TextInfoExtensions
    {
        /// <summary>
        /// Converts the specified <paramref name="text"/> to the selected <see cref="TextInfoCasing"/>.
        /// </summary>
        /// <param name="textInfo">The <see cref="TextInfo"/>.</param>
        /// <param name="text">The text to convert.</param>
        /// <param name="casing">The selected <see cref="TextInfoCasing"/>.</param>
        /// <returns>The converted text.</returns>
        public static string? ToCasing(this TextInfo textInfo, string? text, TextInfoCasing casing) => casing switch
        {
            TextInfoCasing.Lower => text == null ? null : textInfo.ToLower(text),
            TextInfoCasing.Upper => text == null ? null : textInfo.ToUpper(text),
            TextInfoCasing.Title => text == null ? null : textInfo.ToTitleCase(text),
            _ => text
        };
    }
}