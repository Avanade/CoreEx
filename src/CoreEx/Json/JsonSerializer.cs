﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;

namespace CoreEx.Json
{
    /// <summary>
    /// Provides the <see cref="Default"/> <see cref="IJsonSerializer"/> instance.
    /// </summary>
    public static class JsonSerializer
    {
        private static IJsonSerializer? _jsonSerializer = new CoreEx.Text.Json.JsonSerializer();

        /// <summary>
        /// Gets or sets the default <see cref="IJsonSerializer"/> instance.
        /// </summary>
        /// <remarks>Defaults to <see cref="CoreEx.Text.Json.JsonSerializer"/>.</remarks>
        public static IJsonSerializer Default
        {
            get => _jsonSerializer ?? throw new InvalidOperationException($"No default {nameof(IJsonSerializer)} has been defined; this must be set prior to access.");
            set => _jsonSerializer = value.ThrowIfNull(nameof(value));
        }

        /// <summary>
        /// Gets the dictionary of JSON name substitutions that will be used during serialization to rename the .NET property to the specified JSON name.
        /// </summary>
        /// <remarks>The dictionary key is the .NET property name and the value is the corresponding JSON name.</remarks>
        public static Dictionary<string, string> NameSubstitutions { get; } = new Dictionary<string, string> { { "ETag", "etag" } };
    }
}