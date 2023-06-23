// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Localization;
using System;
using System.Net;

namespace CoreEx
{
    /// <summary>
    /// Represents a data <b>Duplicate</b> exception.
    /// </summary>
    /// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A duplicate error occurred.</i></remarks>
    public class DuplicateException : Exception, IExtendedException
    {
        private const string _message = "A duplicate error occurred.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateException"/> class.
        /// </summary>
        public DuplicateException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public DuplicateException(string? message) : base(message ?? new LText(typeof(DuplicateException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DuplicateException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public DuplicateException(string? message, Exception innerException) : base(message ?? new LText(typeof(DuplicateException).FullName, _message), innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.DuplicateError"/> value as a <see cref="string"/>.</returns>
        public string ErrorType => Abstractions.ErrorType.DuplicateError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.DuplicateError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)Abstractions.ErrorType.DuplicateError;

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