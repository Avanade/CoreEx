// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore
{
    /// <summary>
    /// Extension methods for '<c>ToResult</c>' (see <see cref="IActionResult"/>).
    /// </summary>
    public static class ToResultExtensions
    {
        /// <summary>
        /// Converts the <see cref="TransientException"/> into a <see cref="HttpStatusCode.ServiceUnavailable"/> result with the '<c>Retry-After</c>' response header set to '<c>120</c>' seconds. 
        /// </summary>
        /// <param name="tex">The <see cref="TransientException"/>.</param>
        /// <returns>The corresponding <see cref="BadRequestObjectResult"/>.</returns>
        public static IActionResult ToResult(this TransientException tex) 
            => new CustomResult(new ContentResult { Content = tex.Message, ContentType = MediaTypeNames.Text.Plain, StatusCode = (int)HttpStatusCode.ServiceUnavailable }, context =>
            {
                context.HttpContext.Response.Headers.Add("Retry-After", "120");
                return Task.CompletedTask;
            });

        /// <summary>
        /// Converts the <see cref="ValidationException"/> into a <see cref="BadRequestObjectResult"/>.
        /// </summary>
        /// <param name="vex">The <see cref="ValidationException"/>.</param>
        /// <returns>The corresponding <see cref="BadRequestObjectResult"/>.</returns>
        public static IActionResult ToResult(this ValidationException vex)
            => vex.ModelStateDictionary == null || vex.ModelStateDictionary.IsValid ? new BadRequestObjectResult(vex.Message) : new BadRequestObjectResult(vex.ModelStateDictionary);

        /// <summary>
        /// Converts the <see cref="EventPublisherException"/> into an <see cref="HttpStatusCode.InternalServerError"/> result.
        /// </summary>
        /// <param name="epex">The <see cref="EventPublisherException"/>.</param>
        /// <returns>The corresponding <see cref="BadRequestObjectResult"/>.</returns>
        public static IActionResult ToResult(this EventPublisherException epex)
        {
            if (epex.Errors == null || !epex.Errors.Any())
                return new BadRequestObjectResult(epex.Message);

            var msd = new ModelStateDictionary();
            foreach (var item in epex.Errors)
            {
                msd.AddModelError($"value[{item.Index}]", item.Message);
            }

            return new BadRequestObjectResult(msd) { StatusCode = (int)HttpStatusCode.InternalServerError };
        }

        /// <summary>
        /// Converts the <see cref="Exception"/> into a <see cref="HttpStatusCode.InternalServerError"/> result.
        /// </summary>
        /// <param name="ex">The <see cref="Exception"/>.</param>
        /// <param name="includeExceptionInResult">Indicates whether to the include the underlying <see cref="Exception"/> content in the externally returned result.</param>
        /// <returns></returns>
        public static IActionResult ToResult(this Exception ex, bool includeExceptionInResult = false)
            => includeExceptionInResult
                ? new ContentResult { StatusCode = (int) HttpStatusCode.InternalServerError, ContentType = MediaTypeNames.Text.Plain, Content = $"An unexpected internal server error has occurred. CorrelationId={Executor.GetCorrelationId()} Exception={ex}" }
                : new ContentResult { StatusCode = (int) HttpStatusCode.InternalServerError, ContentType = MediaTypeNames.Text.Plain, Content = $"An unexpected internal server error has occurred. CorrelationId={Executor.GetCorrelationId()}" };

        /// <summary>
        /// Converts the <see cref="HttpRequestJsonValueBase"/> into a <see cref="BadRequestObjectResult"/>.
        /// </summary>
        /// <param name="jv">The <see cref="HttpRequestJsonValueBase"/>.</param>
        /// <returns>The corresponding <see cref="BadRequestObjectResult"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when <see cref="HttpRequestJsonValueBase.IsValid"/>.</exception>
        public static IActionResult ToBadRequestResult(this HttpRequestJsonValueBase jv)
        {
            if (jv.IsValid)
                throw new InvalidOperationException($"The request is considered _valid_ and therefore can not be converted into a {nameof(BadRequestObjectResult)}.");

            return jv.ValidationException!.ToResult();
        }
    }
}