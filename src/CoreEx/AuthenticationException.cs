// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace CoreEx
{
    /// <summary>
    /// Represents an <b>Authentication</b> exception.
    /// </summary>
    /// <remarks>The <see cref="Exception.Message"/> defaults to: <i>An authentication error occured; the credentials you provided are not valid.</i></remarks>
    public class AuthenticationException : Exception, IExtendedException
    {
        private const string _message = "An authorization error occurred; you are not permitted to perform this action.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
        /// </summary>
        public AuthenticationException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public AuthenticationException(string? message) : base(message ?? _message) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public AuthenticationException(string? message, Exception innerException) : base(message ?? _message, innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.AuthenticationError"/> value.</returns>
        public ErrorType ErrorType => ErrorType.AuthenticationError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.Forbidden"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.Forbidden;

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