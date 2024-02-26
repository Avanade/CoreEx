// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Newtonsoft.Json.Linq;
using Nsj = Newtonsoft.Json;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Provides pre (prior) to filtering JSON inspection.
    /// </summary>
    /// <param name="json">The <see cref="JToken"/>.</param>
    public readonly struct JsonPreFilterInspector(JToken json) : IJsonPreFilterInspector
    {
        /// <inheritdoc/>
        object IJsonPreFilterInspector.Json => Json;

        /// <summary>
        /// Gets the <see cref="JToken"/> before any filtering has been applied.
        /// </summary>
        public JToken Json { get; } = json;

        /// <inheritdoc/>
        public string? ToJsonString() => Json.ToString(Nsj.Formatting.None);
    }
}