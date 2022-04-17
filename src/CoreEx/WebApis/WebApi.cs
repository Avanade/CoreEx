// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Json;
using CoreEx.Json.Merge;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mime;
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
        /// <param name="jsonMergePatch">The <see cref="IJsonMergePatch"/> to support the <see cref="HttpMethods.Patch"/> operations.</param>
        public WebApi(ExecutionContext executionContext, SettingsBase settings, IJsonSerializer jsonSerializer, ILogger<WebApi> logger, IJsonMergePatch? jsonMergePatch = null)
            : base(executionContext, settings, jsonSerializer, logger) => JsonMergePatch = jsonMergePatch;

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
        /// Gets the <see cref="IJsonMergePatch"/>.
        /// </summary>
        public IJsonMergePatch? JsonMergePatch { get; }

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
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> PostAsync(HttpRequest request, Func<WebApiParam, Task> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create, Func<Uri>? locationUri = null)
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
                return new ExtendedStatusCodeResult(statusCode) { Location = locationUri?.Invoke() };
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
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> PostAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task> function, HttpStatusCode statusCode = HttpStatusCode.Created, OperationType operationType = OperationType.Create, bool valueIsRequired = true, Func<Uri>? locationUri = null)
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
                return new ExtendedStatusCodeResult(statusCode) { Location = locationUri?.Invoke() };
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
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> PostAsync<TResult>(HttpRequest request, Func<WebApiParam, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.Created, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Create, Func<TResult, Uri>? locationUri = null)
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
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: locationUri?.Invoke(result));
            }, operationType).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/>.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> PostAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.Created, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Create, bool valueIsRequired = true, Func<TResult, Uri>? locationUri = null)
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
                return ValueContentResult.CreateResult(result, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: locationUri?.Invoke(result));
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
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> PutAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, Task<TResult>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true)
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

        #region Patch

        /// <summary>
        /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value.
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value to patch into. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace the patched value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>Currently on the <see cref="JsonMergePatch"/> is supported.</remarks>
        public async Task<IActionResult> PatchAsync<TValue>(HttpRequest request, Func<WebApiParam, Task<TValue?>> get, Func<WebApiParam<TValue>, Task<TValue>> put, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Update) where TValue : class
        {
            if (JsonMergePatch == null)
                throw new InvalidOperationException($"To use the '{nameof(PatchAsync)}' methods the '{nameof(JsonMergePatch)}' object must be passed in the constructor. Where using dependency injection consider using '{nameof(Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions.AddJsonMergePatch)}' to add and configure the supported options.");

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPatch(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Patch}' to use {nameof(PatchAsync)}.", nameof(request));

            if (get == null)
                throw new ArgumentNullException(nameof(get));

            if (put == null)
                throw new ArgumentNullException(nameof(put));

            return await RunAsync(request, async wap =>
            {
                // Make sure that the only the support content types are used.
                var ct = request.GetTypedHeaders()?.ContentType?.MediaType.Value;
                if (StringComparer.OrdinalIgnoreCase.Compare(ct, HttpConsts.MergePatchMediaTypeName) != 0 && StringComparer.OrdinalIgnoreCase.Compare(ct, MediaTypeNames.Application.Json) != 0)
                    return new ContentResult 
                    { 
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        ContentType = MediaTypeNames.Text.Plain, 
                        Content = $"Unsupported 'Content-Type' for a PATCH; only JSON Merge Patch is supported using either: 'application/merge-patch+json' or '{MediaTypeNames.Application.Json}'."
                    };

                // Retrieve the JSON content string; there must be some content of some type.
                var json = await request.ReadAsStringAsync(true).ConfigureAwait(false);
                if (json.Exception != null)
                    return json.Exception.ToResult();

                // Note: the JsonMergePatch will throw JsonMergePatchException on error which will be automatically converted to an appropriate IActionResult by the invoking RunAsync method.
                TValue? value = null;
                var changed = await JsonMergePatch.MergeAsync<TValue>(json.Content!, async jpv =>
                {
                    // Get the current value before we perform the merge.
                    value = await get(wap).ConfigureAwait(false);
                    if (value == null)
                        throw new NotFoundException();

                    // Where etags are supported then we need to make sure one was provided up-front.
                    string? etag = null;
                    if (jpv is IETag et)
                    {
                        etag = et.ETag ?? wap.RequestOptions.ETag;
                        if (string.IsNullOrEmpty(etag))
                            throw new ConcurrencyException($"An 'If-Match' header is required for an HTTP {HttpMethods.Patch} where the underlying entity supports concurrency (ETag).");
                    }

                    // Where etags are supported then ensure etag match before continuing.
                    if (etag != null && value is IETag vet && vet.ETag != etag)
                        throw new ConcurrencyException();

                    return value;
                }).ConfigureAwait(false);

                // Only invoke the put function where something was *actually* changed.
                if (changed)
                    await put(new WebApiParam<TValue>(wap, value!)).ConfigureAwait(false);

                return ValueContentResult.CreateResult(value, statusCode, null, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType).ConfigureAwait(false);
        }

        #endregion
    }
}