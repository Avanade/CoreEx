// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Wildcards
{
    /// <summary>
    /// Represents the wildcard selection.
    /// </summary>
    [Flags]
    public enum WildcardSelection
    {
        /// <summary>
        /// Indicates that the wildcard selection is undetermined.
        /// </summary>
        Undetermined,

        /// <summary>
        /// Indicates that there was no selection; i.e the text was null or empty (see <see cref="string.IsNullOrEmpty(string)"/>).
        /// </summary>
        None = 1,

        /// <summary>
        /// Indicates that no wildcard characters were found and an equal operation should be performed.
        /// </summary>
        Equal = 2,

        /// <summary>
        /// Indicates a single wildcard character (e.g. '*' or '?').
        /// </summary>
        Single = 4,

        /// <summary>
        /// Indicates the selection contains a starts with operation (e.g. 'xxx*', 'xxx?', 'xx*x*', etc).
        /// </summary>
        StartsWith = 8,

        /// <summary>
        /// Indicates the selection contains an ends with operation (e.g. '*xxx', '?xxx', '?x*xx', etc).
        /// </summary>
        EndsWith = 16,

        /// <summary>
        /// Indicates the selection contains both a <see cref="StartsWith"/> and <see cref="EndsWith"/> (with no <see cref="Embedded"/>) operation (e.g. '*xxx*', '?xxx*', etc).
        /// </summary>
        Contains = 32,

        /// <summary>
        /// Indicates the selection contains an embedded operation (e.g. 'xx*xx', '*xx*xx*', 'xx?xx*', etc).
        /// </summary>
        Embedded = 64,

        /// <summary>
        /// Indicates the selection contains at least one instance of the <see cref="Wildcard.MultiWildcard"/> character.
        /// </summary>
        MultiWildcard = 128,

        /// <summary>
        /// Indicates the selection contains at least one instance of the <see cref="Wildcard.SingleWildcard"/> character.
        /// </summary>
        SingleWildcard = 256,

        /// <summary>
        /// Indicates the selection contains adjacent (side-by-side) wildcard characters (e.g. '*?', 'xx**xx', 'xxx**', etc.
        /// </summary>
        AdjacentWildcards = 512,

        /// <summary>
        /// Indicates the selection contains one or more invalid characters (see <see cref="Wildcard.CharactersNotAllowed"/>).
        /// </summary>
        InvalidCharacter = 1024,

        /// <summary>
        /// Represents the <see cref="MultiWildcard"/> and <see cref="SingleWildcard"/> <b>all</b> selections; excludes <see cref="InvalidCharacter"/> and <see cref="Undetermined"/>.
        /// </summary>
        BothAll = None | Equal | Single | StartsWith | EndsWith | Contains | Embedded | MultiWildcard | SingleWildcard | AdjacentWildcards,

        /// <summary>
        /// Represents the <see cref="MultiWildcard"/> <b>basic</b> selections; includes <see cref="Equal"/>, <see cref="StartsWith"/>, <see cref="EndsWith"/>, <see cref="Contains"/> and <see cref="AdjacentWildcards"/>.
        /// </summary>
        MultiBasic = None | Equal | Single | StartsWith | EndsWith | Contains | MultiWildcard | AdjacentWildcards,

        /// <summary>
        /// Represents the <see cref="MultiWildcard"/> <b>all</b> selections; includes <see cref="MultiBasic"/> and <see cref="Embedded"/>.
        /// </summary>
        MultiAll = MultiBasic | Embedded
    }
}