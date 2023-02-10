// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Globalization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a <see cref="string"/> casing conversion option.
    /// </summary>
    /// <remarks>See <see cref="Cleaner"/>.</remarks>
    public enum StringCase
    {
        /// <summary>
        /// Indicates that the <see cref="Cleaner.DefaultStringCase"/> value should be used.
        /// </summary>
        UseDefault,

        /// <summary>
        /// No casing conversion required; the <see cref="string"/> value will remain as-is.
        /// </summary>
        None,

        /// <summary>
        /// The string value will be converted to lower case (see <see cref="TextInfo.ToLower(string)"/>).
        /// </summary>
        Lower,

        /// <summary>
        /// The string value will be converted to upper case (see <see cref="TextInfo.ToUpper(string)"/>).
        /// </summary>
        Upper,

        /// <summary>
        /// The string value will be converted to title case (see <see cref="TextInfo.ToTitleCase(string)"/>).
        /// </summary>
        Title
    }
}