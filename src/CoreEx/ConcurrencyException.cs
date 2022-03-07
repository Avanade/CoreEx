// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace CoreEx
{
    /// <summary>
    /// Represents a data <b>Concurrency</b> exception.
    /// </summary>
    /// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A concurrency error occurred; please refresh the data and try again.</i></remarks>
    public class ConcurrencyException : Exception, IExtendedException
    {
        private const string _message = "A concurrency error occurred; please refresh the data and try again.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class.
        /// </summary>
        public ConcurrencyException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public ConcurrencyException(string? message) : base(message ?? new LText(typeof(ConcurrencyException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrencyException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public ConcurrencyException(string? message, Exception innerException) : base(message ?? new LText(typeof(ConcurrencyException).FullName, _message), innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.ConcurrencyError"/> value as a <see cref="string"/>.</returns>
        public string ErrorReason => ErrorType.ConcurrencyError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.ConcurrencyError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)ErrorType.ConcurrencyError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.PreconditionFailed"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.PreconditionFailed;

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

        /// <inheritdoc/>
        public IActionResult ToResult() => this.ToResult(StatusCode);
    }
}