// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Wildcards
{
    /// <summary>
    /// Defines the treatment of embedded <see cref="Wildcard.SpaceCharacter"/> within the wildcard.
    /// </summary>
    /// <remarks>Note: leading and trailing spaces are <i>always</i> removed.</remarks>
    public enum WildcardSpaceTreatment
    {
        /// <summary>
        /// Indicates that no treatment is to be performed; leave as is.
        /// </summary>
        None,

        /// <summary>
        /// Indicates that multiple adjacent embedded <see cref="Wildcard.SpaceCharacter"/> are compressed into a single space.
        /// </summary>
        /// <remarks>Example where underscope represents space visually: <c>'XX___XX'</c> -> <c>'XX_XX'</c>.</remarks>
        Compress,

        /// <summary>
        /// Indicates that the embedded <see cref="Wildcard.SpaceCharacter"/> are <i>always</i> compressed and converted to the multi-wildcard character.
        /// </summary>
        /// <remarks>Examples where underscope represents space visually: <c>'XX___XX'</c> -> <c>'XX*XX'</c> and <c>'XX___XX*'</c> -> <c>'XX*XX*'</c>.</remarks>
        MultiWildcardAlways,

        /// <summary>
        /// Indicates that the embedded <see cref="Wildcard.SpaceCharacter"/> are compressed and converted to the multi-wildcard character only where <i>other</i> multi-wildcards are found.
        /// </summary>
        /// <remarks>Examples where underscope represents space visually: <c>'XX___XX'</c> -> <c>'XX___XX'</c> and <c>'XX___XX*'</c> -> <c>'XX*XX*'</c>.</remarks>
        MultiWildcardWhenOthers
    }
}