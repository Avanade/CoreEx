﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Localization;
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
        private const string _message = "An authentication error occurred; the credentials you provided are not valid.";
        private static bool? _shouldExceptionBeLogged;

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get => _shouldExceptionBeLogged ?? Internal.ShouldExceptionBeLogged<AuthenticationException>(); set => _shouldExceptionBeLogged = value; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class.
        /// </summary>
        public AuthenticationException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public AuthenticationException(string? message) : base(message ?? new LText(typeof(AuthenticationException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public AuthenticationException(string? message, Exception innerException) : base(message ?? new LText(typeof(AuthenticationException).FullName, _message), innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.AuthenticationError"/> value as a <see cref="string"/>.</returns>
        public string ErrorType => Abstractions.ErrorType.AuthenticationError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.AuthenticationError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)Abstractions.ErrorType.AuthenticationError;

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
    }
}