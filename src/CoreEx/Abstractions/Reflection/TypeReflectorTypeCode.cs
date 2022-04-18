// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Represents the <see cref="TypeReflector"/> <see cref="Type"/> code.
    /// </summary>
    public enum TypeReflectorTypeCode
    {
        /// <summary>
        /// Is a <c>struct</c> or <see cref="string"/>, i.e. not any of the others.
        /// </summary>
        Simple,

        /// <summary>
        /// Is a complex type being a class with properties (not identified as one of the possible collection types).
        /// </summary>
        Complex,

        /// <summary>
        /// Is an <see cref="System.Array"/>.
        /// </summary>
        Array,

        /// <summary>
        /// Is an <see cref="System.Collections.ICollection"/>.
        /// </summary>
        ICollection,

        /// <summary>
        /// Is an <see cref="System.Collections.IEnumerable"/>.
        /// </summary>
        IEnumerable,

        /// <summary>
        /// Is an <see cref="System.Collections.IDictionary"/>.
        /// </summary>
        IDictionary,
    }
}