// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Text.Json.Nodes;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides JSON extension methods.
    /// </summary>
    public static class JsonExtensions
    {
        /// <summary>
        /// Applies the inclusion of properties (using JSON paths) to an existing <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonNode"/>.</param>
        /// <param name="pathsToInclude">The list of paths to include (i.e. remove others). Qualified paths, that include indexing, are also supported.</param>
        /// <returns>The <paramref name="json"/> to enable fluent-style method-chaining.</returns>
        /// <remarks>Defaults to <see cref="StringComparison.OrdinalIgnoreCase"/> to match <paramref name="pathsToInclude"/>. Leverages the <see cref="JsonFilterer.Apply"/> to perform.</remarks>
        public static JsonNode ApplyInclude(this JsonNode json, params string[] pathsToInclude) => JsonFilterer.Apply(json, pathsToInclude, JsonPropertyFilter.Include);

        /// <summary>
        /// Applies the exclusion of properties (using JSON paths) to an existing <see cref="JsonNode"/>.
        /// </summary>
        /// <param name="json">The <see cref="JsonNode"/>.</param>
        /// <param name="pathsToExclude">The list of paths to exclude (i.e. remove listed). Qualified paths, that include indexing, are also supported.</param>
        /// <returns>The <paramref name="json"/> to enable fluent-style method-chaining.</returns>
        /// <remarks>Defaults to <see cref="StringComparison.OrdinalIgnoreCase"/> to match <paramref name="pathsToExclude"/>. Leverages the <see cref="JsonFilterer.Apply"/> to perform.</remarks>
        public static JsonNode ApplyExclude(this JsonNode json, params string[] pathsToExclude) => JsonFilterer.Apply(json, pathsToExclude, JsonPropertyFilter.Exclude);
    }
}