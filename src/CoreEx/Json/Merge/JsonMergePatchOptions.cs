// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// The <see cref="JsonMergePatch"/> options.
    /// </summary>
    /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
    public class JsonMergePatchOptions(IJsonSerializer? jsonSerializer = null)
    {
        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        public IJsonSerializer JsonSerializer { get; } = jsonSerializer ?? ExecutionContext.GetService<IJsonSerializer>() ?? Json.JsonSerializer.Default;

        /// <summary>
        /// Gets or sets the <see cref="StringComparer"/> for matching the JSON name (defaults to <see cref="StringComparer.OrdinalIgnoreCase"/>).
        /// </summary>
        public StringComparer PropertyNameComparer { get; set; } = StringComparer.OrdinalIgnoreCase;
    }
}