// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Json
{
    /// <summary>
    /// Provides the <see cref="Default"/> <see cref="IJsonSerializer"/> instance.
    /// </summary>
    public static class DefaultSerializer
    {
        private static IJsonSerializer? _jsonSerializer;

        /// <summary>
        /// Gets or sets the default <see cref="IJsonSerializer"/> instance.
        /// </summary>
        public static IJsonSerializer Default
        {
            get => _jsonSerializer ?? throw new InvalidOperationException($"No default {nameof(IJsonSerializer)} has been defined; this must be set prior to access.");
            set => _jsonSerializer = value ?? throw new ArgumentNullException(nameof(value));
        }
    }
}