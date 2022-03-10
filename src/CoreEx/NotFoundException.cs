// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace CoreEx
{
    /// <summary>
    /// Represents a <b>Not Found</b> exception.
    /// </summary>
    /// <remarks>The <see cref="Exception.Message"/> defaults to: <i>Requested data was not found.</i></remarks>
    public class NotFoundException : Exception, IExtendedException
    {
        private const string _message = "Requested data was not found.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class.
        /// </summary>
        public NotFoundException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public NotFoundException(string? message) : base(message ?? new LText(typeof(NotFoundException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="NotFoundException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public NotFoundException(string? message, Exception innerException) : base(message ?? new LText(typeof(NotFoundException).FullName, _message), innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.NotFoundError"/> value as a <see cref="string"/>.</returns>
        public string ErrorType => Abstractions.ErrorType.NotFoundError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.NotFoundError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)Abstractions.ErrorType.NotFoundError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.NotFound"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.NotFound;

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