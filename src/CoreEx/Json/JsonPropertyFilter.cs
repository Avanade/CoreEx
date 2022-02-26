// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Json
{
    /// <summary>
    /// Defines the JSON property filter (<see cref="Include"/> or <see cref="Exclude"/>) for the <see cref="IJsonSerializer.TryApplyFilter{T}(T, IEnumerable{string}?, out object, JsonPropertyFilter, IEqualityComparer{string}?)"/>
    /// and <see cref="IJsonSerializer.TryApplyFilter{T}(T, IEnumerable{string}?, out string, JsonPropertyFilter, IEqualityComparer{string}?)"/>.
    /// </summary>
    public enum JsonPropertyFilter
    {
        /// <summary>
        /// Indicates whether to <i>include</i> only those properties that have been specified.
        /// </summary>
        Include,

        /// <summary>
        /// Indicates whether to <i>exclude</i> those properties that have been specified.
        /// </summary>
        Exclude
    }
}