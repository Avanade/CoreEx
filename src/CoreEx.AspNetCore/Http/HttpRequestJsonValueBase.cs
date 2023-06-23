// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.AspNetCore.Http
{
    /// <summary>
    /// Represents the base for a <see cref="HttpRequestJsonValue"/> and <see cref="HttpRequestJsonValue{T}"/>.
    /// </summary>
    public abstract class HttpRequestJsonValueBase
    {
        /// <summary>
        /// Indicates whether the request value was found to be valid.
        /// </summary>
        public bool IsValid => ValidationException == null;

        /// <summary>
        /// Indicates whether the request value was found to be invalid.
        /// </summary>
        public bool IsInvalid => !IsValid;

        /// <summary>
        /// Gets or sets any corresponding <see cref="Exception"/> related to validation.
        /// </summary>
        /// <remarks>This is typically set as the result of JSON deserialization.</remarks>
        public Exception? ValidationException { get; set; }
    }
}