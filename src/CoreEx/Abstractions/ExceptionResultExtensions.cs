// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Mime;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Enables the extended exception capabilities.
    /// </summary>
    public static class ExceptionResultExtensions
    {
        /// <summary>
        /// Converts the <paramref name="exception"/> into a corresponding <see cref="ContentResult"/> with the specified <paramref name="statusCode"/>.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
        /// <returns>The corresponding <see cref="ContentResult"/>.</returns>
        public static IActionResult ToResult(this Exception exception, HttpStatusCode statusCode)
            => new ContentResult { StatusCode = (int)statusCode, ContentType = MediaTypeNames.Text.Plain, Content = exception.Message };

        /// <summary>
        /// Converts the unexpected <paramref name="exception"/> into a corresponding <see cref="HttpStatusCode.InternalServerError"/> <see cref="ContentResult"/>.
        /// </summary>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <param name="includeExceptionInResult">Indicates whether to the include the underlying <paramref name="exception"/> content in the result.</param>
        /// <returns>The corresponding <see cref="ContentResult"/>.</returns>
        public static IActionResult ToUnexpectedResult(this Exception exception, bool includeExceptionInResult) => includeExceptionInResult
            ? new ContentResult { StatusCode = (int)HttpStatusCode.InternalServerError, ContentType = MediaTypeNames.Text.Plain, Content = $"An unexpected internal server error has occurred. CorrelationId={ExecutionContext.Current.CorrelationId} Exception={exception}" }
            : new ContentResult { StatusCode = (int)HttpStatusCode.InternalServerError, ContentType = MediaTypeNames.Text.Plain, Content = $"An unexpected internal server error has occurred. CorrelationId={ExecutionContext.Current.CorrelationId}" };
    }
}