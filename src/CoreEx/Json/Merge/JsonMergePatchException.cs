// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx.Json.Merge
{
    /// <summary>
    /// Represents a <see cref="JsonMergePatch"/> <see cref="ValidationException"/>.
    /// </summary>
    /// <remarks>Inherits from <see cref="ValidationException"/> as the <see cref="ValidationException.StatusCode"/> and related handling are the same.</remarks>
    public class JsonMergePatchException : ValidationException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMergePatchException"/> class.
        /// </summary>
        public JsonMergePatchException() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMergePatchException"/> class with the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public JsonMergePatchException(string message) : base(message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonMergePatchException"/> class with the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public JsonMergePatchException(string message, Exception innerException) : base(message, innerException) { }
    }
}