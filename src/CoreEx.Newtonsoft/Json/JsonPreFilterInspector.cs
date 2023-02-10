// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Newtonsoft.Json.Linq;
using Nsj = Newtonsoft.Json;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Provides pre (prior) to filtering JSON inspection.
    /// </summary>
    public readonly struct JsonPreFilterInspector : IJsonPreFilterInspector
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonPreFilterInspector"/> struct. 
        /// </summary>
        /// <param name="json">The <see cref="JToken"/>.</param>
        public JsonPreFilterInspector(JToken json) => Json = json;

        /// <inheritdoc/>
        object IJsonPreFilterInspector.Json => Json;

        /// <summary>
        /// Gets the <see cref="JToken"/> before any filtering has been applied.
        /// </summary>
        public JToken Json { get; }

        /// <inheritdoc/>
        public string? ToJsonString() => Json.ToString(Nsj.Formatting.None);
    }
}