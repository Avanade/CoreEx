// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.AspNetCore.Http
{
    /// <summary>
    /// Represents a <see cref="Type"/> <typeparamref name="T"/> HTTP request JSON-deserialized <see cref="Value"/>.
    /// </summary>
    /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
    public class HttpRequestJsonValue<T> : HttpRequestJsonValueBase
    {
        /// <summary>
        /// Gets or sets the deserialized request value.
        /// </summary>
        public T Value { get; set; } = default!;
    }
}