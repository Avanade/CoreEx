// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Localization;
using System;
using System.Net;

namespace CoreEx
{
    /// <summary>
    /// Represents an <b>Conflict</b> exception.
    /// </summary>
    /// <remarks>An example would be where the identifier provided for a Create operation already exists.
    /// <para>The <see cref="Exception.Message"/> defaults to: <i>A data conflict occurred.</i></para></remarks>
    public class ConflictException : Exception, IExtendedException
    {
        private const string _message = "A data conflict occurred.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class.
        /// </summary>
        public ConflictException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ConflictException(string? message) : base(message ?? new LText(typeof(ConflictException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConflictException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public ConflictException(string? message, Exception innerException) : base(message ?? new LText(typeof(ConflictException).FullName, _message), innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.ConflictError"/> value as a <see cref="string"/>.</returns>
        public string ErrorType => Abstractions.ErrorType.ConflictError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.ConflictError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)Abstractions.ErrorType.ConflictError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.Conflict"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.Conflict;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><c>false</c>; is not considered transient.</returns>
        public bool IsTransient => false;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ShouldExceptionBeLogged"/> value.</returns>
        public bool ShouldBeLogged => ShouldExceptionBeLogged;
    }
}