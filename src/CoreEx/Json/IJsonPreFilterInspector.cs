// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Json
{
    /// <summary>
    /// Defines pre (prior) to filtering JSON inspector.
    /// </summary>
    /// <remarks>Enables access to the serialized value before the filtering occurs.</remarks>
    public interface IJsonPreFilterInspector
    {
        /// <summary>
        /// Gets the underlying JSON object (as per the underlying implementation).
        /// </summary>
        object Json { get; }

        /// <summary>
        /// Returns the JSON string.
        /// </summary>
        /// <returns>The JSON string.</returns>
        string? ToJsonString();
    }
}