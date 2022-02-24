// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a dynamic HTTP request JSON-deserialized <see cref="Value"/>.
    /// </summary>
    public class HttpRequestJsonValue : HttpRequestJsonValueBase
    {
        /// <summary>
        /// Gets or sets the deserialized request value.
        /// </summary>
        public object? Value { get; set; }
    }
}