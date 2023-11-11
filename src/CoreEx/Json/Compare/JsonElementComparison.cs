// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Text.Json;

namespace CoreEx.Json.Compare
{
    /// <summary>
    /// Defines the <see cref="JsonElement"/> comparison option where <see cref="JsonElement.ValueKind"/> is either a <see cref="JsonValueKind.String"/> or <see cref="JsonValueKind.Number"/>.
    /// </summary>
    public enum JsonElementComparison
    {
        /// <summary>
        /// Indicates that a semantic match is to used for the comparison.
        /// </summary>
        /// <remarks>Where <see cref="JsonValueKind.String"/> the comparison will be performed using <see cref="JsonElement.GetDateTimeOffset"/>, <see cref="JsonElement.GetDateTime"/>, <see cref="JsonElement.GetGuid"/> and <see cref="JsonElement.GetString"/> (in order specified) until match found;
        /// otherwise, for a <see cref="JsonValueKind.Number"/> the comparison will be performed using <see cref="JsonElement.GetDecimal"/> and <see cref="JsonElement.GetDouble"/> (in order specified) until a match found.</remarks>
        Semantic,

        /// <summary>
        /// Indicates that an exact match is to used for the comparison.
        /// </summary>
        /// <remarks>Uses the <see cref="JsonElement.GetRawText"/> for the value comparison.</remarks>
        Exact
    }
}