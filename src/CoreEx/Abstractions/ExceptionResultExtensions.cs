// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx.Abstractions
{
    /// <summary>
    /// Enables the extended exception capabilities.
    /// </summary>
    public static class ExceptionResultExtensions
    {
        /// <summary>
        /// Converts the <paramref name="exception"/> into a corresponding <see cref="ExtendedContentResult"/>.
        /// </summary>
        /// <param name="exception">The <see cref="IExtendedException"/>.</param>
        /// <param name="statusCode">The optional overridding <see cref="HttpStatusCode"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedContentResult"/>.</returns>
        public static IActionResult ToResult(this IExtendedException exception, HttpStatusCode? statusCode) => new ExtendedContentResult
        {
            Content = exception.Message,
            ContentType = MediaTypeNames.Text.Plain,
            StatusCode = (int)(statusCode ?? exception.StatusCode),
            BeforeExtension = r =>
            {
                var th = r.GetTypedHeaders();
                th.Set(HttpConsts.ErrorTypeHeaderName, exception.ErrorType);
                th.Set(HttpConsts.ErrorCodeHeaderName, exception.ErrorCode.ToString());
                return Task.CompletedTask;
            }
        };

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