// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace CoreEx
{
    /// <summary>
    /// Represents an <b>Authorization</b> exception.
    /// </summary>
    /// <remarks>The <see cref="Exception.Message"/> defaults to: <i>An authorization error occurred; you are not permitted to perform this action.</i></remarks>
    public class AuthorizationException : Exception, IExtendedException
    {
        private const string _message = "An authorization error occurred; you are not permitted to perform this action.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class.
        /// </summary>
        public AuthorizationException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public AuthorizationException(string? message) : base(message ?? new LText(typeof(AuthorizationException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthorizationException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public AuthorizationException(string? message, Exception innerException) : base(message ?? new LText(typeof(AuthorizationException).FullName, _message), innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.AuthorizationError"/> value as a <see cref="string"/>.</returns>
        public string ErrorType => Abstractions.ErrorType.AuthorizationError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.AuthorizationError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)Abstractions.ErrorType.AuthorizationError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.Unauthorized"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.Unauthorized;

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