// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Http;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Threading.Tasks;

namespace CoreEx.WebApis
{
    /// <summary>
    /// Provides the core (<see cref="HttpMethods.Get"/>, <see cref="HttpMethods.Post"/>, <see cref="HttpMethods.Put"/> and <see cref="HttpMethods.Delete"/>) Web API execution encapsulation.
    /// </summary>
    public class WebApi : WebApiBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApi"/> class.
        /// </summary>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public WebApi(ExecutionContext executionContext, SettingsBase settings, IJsonSerializer jsonSerializer, ILogger<WebApi> logger)
            : base(executionContext, settings, jsonSerializer, logger) { }

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> returning a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        /// <remarks>This is, and must be, used by all methods that process an <see cref="HttpRequest"/> to ensure that the standardized before and after, success and error, handling occurs as required.</remarks>
        public new Task<IActionResult> RunAsync(HttpRequest request, Func<WebApiParam, Task<IActionResult>> function, OperationType operationType = OperationType.Unspecified)
            => base.RunAsync(request, function, operationType);

        /// <summary>
        /// Encapsulates the execution of an <see cref="HttpRequest"/> <paramref name="function"/> with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding <see cref="IActionResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function logic to invoke.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <returns>The resulting <see cref="IActionResult"/>.</returns>
        public async Task<IActionResult> RunAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task<IActionResult>> function, OperationType operationType = OperationType.Unspecified, bool valueIsRequired = true)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                var vr = await request.ReadAsJsonValueAsync<TValue>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                return await function(new WebApiParam<TValue>(wap, vr.Value)).ConfigureAwait(false);
            }, operationType).ConfigureAwait(false);
        }

        #region GetAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation returning a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> GetAsync<TResult>(HttpRequest request, Func<WebApiParam, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NotFound, OperationType operationType = OperationType.Read)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsGet(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Get}' to use {nameof(GetAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                var result = await function(wap).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: true, location: null);
            }, operationType).ConfigureAwait(false);
        }

        #endregion

        #region Post

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request content or response value.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> PostAsync(HttpRequest request, Func<WebApiParam, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                await function(wap).ConfigureAwait(false);
                return new ExtendedStatusCodeResult(statusCode);
            }, operationType).ConfigureAwait(false);                                                   
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> PostAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task> function, HttpStatusCode statusCode = HttpStatusCode.Created, OperationType operationType = OperationType.Create, bool valueIsRequired = true)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                var vr = await request.ReadAsJsonValueAsync<TValue>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                await function(new WebApiParam<TValue>(wap, vr.Value)).ConfigureAwait(false);
                return new ExtendedStatusCodeResult(statusCode);
            }, operationType).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request body and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> PostAsync<TResult>(HttpRequest request, Func<WebApiParam, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.Created, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Create)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                var result = await function(wap).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> PostAsync<TResult, TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.Created, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Create, bool valueIsRequired = true)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                var vr = await request.ReadAsJsonValueAsync<TValue>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                var result = await function(new WebApiParam<TValue>(wap, vr.Value)).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType).ConfigureAwait(false);
        }

        #endregion

        #region Put

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> PutAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPut(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Put}' to use {nameof(PutAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                var vr = await request.ReadAsJsonValueAsync<TValue>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                await function(new WebApiParam<TValue>(wap, vr.Value)).ConfigureAwait(false);
                return new ExtendedStatusCodeResult(statusCode);
            }, operationType).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> PutAsync<TResult, TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPut(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Put}' to use {nameof(PutAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                var vr = await request.ReadAsJsonValueAsync<TValue>(JsonSerializer, valueIsRequired).ConfigureAwait(false);
                if (vr.IsInvalid)
                    return vr.ToBadRequestResult();

                var result = await function(new WebApiParam<TValue>(wap, vr.Value)).ConfigureAwait(false);
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType).ConfigureAwait(false);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Performs a <see cref="HttpMethods.Delete"/> operation.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> DeleteAsync(HttpRequest request, Func<WebApiParam, Task> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Delete)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsDelete(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Delete}' to use {nameof(DeleteAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async wap =>
            {
                await function(wap).ConfigureAwait(false);
                return new ExtendedStatusCodeResult(statusCode);
            }, operationType).ConfigureAwait(false);
        }

        #endregion
    }
}