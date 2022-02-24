// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace CoreEx
{
    /// <summary>
    /// Represents a known transient error; i.e. is a candidate for a retry.
    /// </summary>
    public class TransientException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class.
        /// </summary>
        public TransientException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public TransientException(string? message) : base(message ?? "A transient error has occurred; please try again.") { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public TransientException(string? message, Exception innerException) : base(message ?? "A transient error has occurred; please try again.", innerException) { }
    }
}