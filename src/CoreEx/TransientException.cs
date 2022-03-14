// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.AspNetCore;
using CoreEx.Localization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx
{
    /// <summary>
    /// Represents a <b>Transient</b> exception; i.e. is a candidate for a retry.
    /// </summary>
    /// <remarks>The <see cref="Exception.Message"/> defaults to: <i>A data validation error occurred.</i></remarks>
    public class TransientException : Exception, IExtendedException
    {
        private const string _message = "A transient error has occurred; please try again.";

        /// <summary>
        /// Get or sets the <see cref="ShouldBeLogged"/> value.
        /// </summary>
        public static bool ShouldExceptionBeLogged { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class.
        /// </summary>
        public TransientException() : this(null!) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class using the specified <paramref name="message"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        public TransientException(string? message) : base(message ?? new LText(typeof(TransientException).FullName, _message)) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransientException"/> class using the specified <paramref name="message"/> and <paramref name="innerException"/>.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The inner <see cref="Exception"/>.</param>
        public TransientException(string? message, Exception innerException) : base(message ?? new LText(typeof(TransientException).FullName, _message), innerException) { }

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.TransientError"/> value as a <see cref="string"/>.</returns>
        public string ErrorType => Abstractions.ErrorType.TransientError.ToString();

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ErrorType.TransientError"/> value as a <see cref="string"/>.</returns>
        public int ErrorCode => (int)Abstractions.ErrorType.TransientError;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="HttpStatusCode.ServiceUnavailable"/> value.</returns>
        public HttpStatusCode StatusCode => HttpStatusCode.ServiceUnavailable;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns><c>true</c>; is considered transient.</returns>
        public bool IsTransient => true;

        /// <summary>
        /// <inheritdoc/>
        /// </summary>
        /// <returns>The <see cref="ShouldExceptionBeLogged"/> value.</returns>
        public bool ShouldBeLogged => ShouldExceptionBeLogged;

        /// <inheritdoc/>
        public IActionResult ToResult() => new ExtendedContentResult
        {
            Content = Message,
            ContentType = MediaTypeNames.Text.Plain,
            StatusCode = (int)StatusCode,
            // TODO: Add back once UnitTestEx is updated to handle IJsonSerializer
            //BeforeExtension = r =>
            //{
            //    r.GetTypedHeaders().Set(HeaderNames.RetryAfter, "120");
            //    return Task.CompletedTask;
            //}
        };
    }
}