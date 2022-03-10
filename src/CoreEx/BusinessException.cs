// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Localization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace CoreEx
{
    /// <summary>
    /// Represents a <b>Business</b> exception.
    /// </summary>
    /// <remarks>This is typically used for a business-oriented error that should be returned to the consumer.
    /// <para>The <see cref="Exception.Message"/> defaults to: <i>A business error occurred.</i></para></remarks>
    public class BusinessException : Exception, IExtendedException
    {
        private const string _message = "A business error occurred.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class.
        /// </summary>
        public BusinessException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public BusinessException(string? message) : base(message ?? new LText(typeof(BusinessException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public BusinessException(string? message, Exception innerException) : base(message ?? new LText(typeof(BusinessException).FullName, _message), innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.BusinessError"/> value as a <see cref="string"/>.</returns>
        public string ErrorType => Abstractions.ErrorType.BusinessError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.BusinessError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)Abstractions.ErrorType.BusinessError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.BadRequest"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.BadRequest;

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