// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents the trimming of white space characters from a <see cref="string"/>.
    /// </summary>
    /// <remarks>See <see cref="Cleaner"/>.</remarks>
    public enum StringTrim
    {
        /// <summary>
        /// Indicates that the <see cref="Cleaner.DefaultStringTrim"/> value should be used.
        /// </summary>
        UseDefault,

        /// <summary>
        /// The string is left unchanged.
        /// </summary>
        None,

        /// <summary>
        /// Removes all occurences of white space characters from the beginning and ending of a string; i.e. <see cref="string.Trim()"/>
        /// </summary>
        Both,

        /// <summary>
        /// Removes all occurences of white space characters from the beginning of a string; i.e. <see cref="string.TrimStart()"/>
        /// </summary>
        Start,

        /// <summary>
        /// Removes all occurences of white space characters from the end of a string; i.e. <see cref="string.TrimEnd()"/>
        /// </summary>
        End
    }
}