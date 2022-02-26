// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Globalization;

namespace CoreEx.Globalization
{
    /// <summary>
    /// Provides the <see cref="TextInfo"/> casing selection.
    /// </summary>
    public enum TextInfoCasing
    {
        /// <summary>
        /// No text casing is to be applied.
        /// </summary>
        None,

        /// <summary>
        /// Use <see cref="TextInfo.ToLower(string)"/>.
        /// </summary>
        Lower,

        /// <summary>
        /// Use <see cref="TextInfo.ToUpper(string)"/>.
        /// </summary>
        Upper
    }
}