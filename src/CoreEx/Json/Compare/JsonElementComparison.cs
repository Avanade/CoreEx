// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Text.Json;

namespace CoreEx.Json.Compare
{
    /// <summary>
    /// Defines the <see cref="JsonElement"/> comparison option where <see cref="JsonElement.ValueKind"/> is either a <see cref="JsonValueKind.String"/>, <see cref="JsonValueKind.Number"/> or <see cref="JsonValueKind.Null"/>.
    /// </summary>
    public enum JsonElementComparison
    {
        /// <summary>
        /// Indicates that a semantic match is to used for the comparison.
        /// </summary>
        Semantic,

        /// <summary>
        /// Indicates that an exact match is to used for the comparison.
        /// </summary>
        /// <remarks>Uses the <see cref="JsonElement.GetRawText"/> for the value comparison.</remarks>
        Exact
    }
}