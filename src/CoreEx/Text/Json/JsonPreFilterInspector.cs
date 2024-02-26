// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System.Text.Json.Nodes;

namespace CoreEx.Text.Json
{
    /// <summary>
    /// Provides pre (prior) to filtering JSON inspection.
    /// </summary>
    /// <param name="json">The <see cref="JsonNode"/>.</param>
    public readonly struct JsonPreFilterInspector(JsonNode json) : IJsonPreFilterInspector
    {
        /// <inheritdoc/>
        object IJsonPreFilterInspector.Json => Json;

        /// <summary>
        /// Gets the <see cref="JsonNode"/> before any filtering has been applied.
        /// </summary>
        public JsonNode Json { get; } = json;

        /// <inheritdoc/>
        public string? ToJsonString() => Json.ToJsonString();
    }
}