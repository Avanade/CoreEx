// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Http;
using CoreEx.Results;
using CoreEx.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.WebApis
{
    public partial class WebApi
    {
        #region GetWithResultAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation returning a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> GetWithResultAsync<TResult>(HttpRequest request, Func<WebApiParam, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NotFound, OperationType operationType = OperationType.Read)
            => GetWithResultAsync(request, (p, _) => function(p), statusCode, alternateStatusCode, operationType, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Get"/> operation returning a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TResult">The result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> GetWithResultAsync<TResult>(HttpRequest request, Func<WebApiParam, CancellationToken, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NotFound, OperationType operationType = OperationType.Read, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsGet(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Get}' to use {nameof(GetAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var result = await function(wap, ct).ConfigureAwait(false);
                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false)
                    : ValueContentResult.CreateResult(result.Value, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: true, location: null);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region PostWithResultAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request content or response value (with a <see cref="Result"/>).
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        public Task<IActionResult> PostWithResultAsync(HttpRequest request, Func<WebApiParam, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create, Func<Uri>? locationUri = null)
            => PostWithResultAsync(request, (p, _) => function(p), statusCode, operationType, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request content or response value (with a <see cref="Result"/>).
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> PostWithResultAsync(HttpRequest request, Func<WebApiParam, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create,
            Func<Uri>? locationUri = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var result = await function(wap, ct).ConfigureAwait(false);
                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false)
                    : new ExtendedStatusCodeResult(statusCode) { Location = locationUri?.Invoke() };
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PostWithResultAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create,
            bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null)
            => PostWithResultAsync(request, (p, _) => function(p), statusCode, operationType, valueIsRequired, validator, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PostWithResultAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create,
            bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null, CancellationToken cancellationToken = default)
            => PostWithResultInternalAsync(request, false, default!, function, statusCode, operationType, valueIsRequired, validator, locationUri, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PostWithResultAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.OK, OperationType operationType = OperationType.Create,
            bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null)
            => PostWithResultAsync(request, value, (p, _) => function(p), statusCode, operationType, valueIsRequired, validator, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PostWithResultAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null, CancellationToken cancellationToken = default)
            => PostWithResultInternalAsync(request, true, value, function, statusCode, operationType, valueIsRequired, validator, locationUri, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        private async Task<IActionResult> PostWithResultInternalAsync<TValue>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<Uri>? locationUri = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, valueIsRequired, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                var result = await function(wapv!, ct).ConfigureAwait(false);
                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false)
                    : new ExtendedStatusCodeResult(statusCode) { Location = locationUri?.Invoke() };
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request body and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostWithResultAsync<TResult>(HttpRequest request, Func<WebApiParam, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Create, Func<TResult, Uri>? locationUri = null)
            => PostWithResultAsync(request, (p, _) => function(p), statusCode, alternateStatusCode, operationType, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with no request body and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public async Task<IActionResult> PostWithResultAsync<TResult>(HttpRequest request, Func<WebApiParam, CancellationToken, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Create, Func<TResult, Uri>? locationUri = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var result = await function(wap, ct).ConfigureAwait(false);
                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false)
                    : ValueContentResult.CreateResult(result.Value, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: locationUri?.Invoke(result));
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostWithResultAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null)
            => PostWithResultAsync(request, (p, _) => function(p), statusCode, alternateStatusCode, operationType, valueIsRequired, validator, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostWithResultAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null, CancellationToken cancellationToken = default)
            => PostWithResultInternalAsync(request, false, default!, function, statusCode, alternateStatusCode, operationType, valueIsRequired, validator, locationUri, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostWithResultAsync<TValue, TResult>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null)
            => PostWithResultAsync(request, value, (p, _) => function(p), statusCode, alternateStatusCode, operationType, valueIsRequired, validator, locationUri, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="locationUri">The optional function to set the location <see cref="Uri"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PostWithResultAsync<TValue, TResult>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null, CancellationToken cancellationToken = default)
            => PostWithResultInternalAsync(request, true, value, function, statusCode, alternateStatusCode, operationType, valueIsRequired, validator, locationUri, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Post"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        private async Task<IActionResult> PostWithResultInternalAsync<TValue, TResult>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK, HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Create, bool valueIsRequired = true, IValidator<TValue>? validator = null, Func<TResult, Uri>? locationUri = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPost(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Post}' to use {nameof(PostAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, valueIsRequired, validator, ct).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                var result = await function(wapv!, ct).ConfigureAwait(false);
                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false)
                    : ValueContentResult.CreateResult(result.Value, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: locationUri?.Invoke(result));
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region PutWithResultAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutWithResultAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => PutWithResultAsync(request, (p, _) => function(p), statusCode, operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutWithResultAsync<TValue>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PutWithResultInternalAsync(request, false, default!, function, statusCode, operationType, valueIsRequired, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutWithResultAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => PutWithResultInternalAsync(request, true, value, (p, _) => function(p), statusCode, operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutWithResultAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PutWithResultInternalAsync(request, true, value, function, statusCode, operationType, valueIsRequired, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and no corresponding response value (with a <see cref="Result"/>).
        /// </summary>
        private async Task<IActionResult> PutWithResultInternalAsync<TValue>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent,
            OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPut(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Put}' to use {nameof(PutWithResultAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, valueIsRequired, validator, cancellationToken).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                var result = await function(wapv!, ct).ConfigureAwait(false);
                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false)
                    : new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PutWithResultAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => PutWithResultAsync(request, (p, _) => function(p), statusCode, alternateStatusCode, operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PutWithResultAsync<TValue, TResult>(HttpRequest request, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PutWithResultInternalAsync(request, false, default!, function, statusCode, alternateStatusCode, operationType, valueIsRequired, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PutWithResultAsync<TValue, TResult>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null)
            => PutWithResultAsync(request, value, (p, _) => function(p), statusCode, alternateStatusCode, operationType, valueIsRequired, validator, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResult">The response result <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="alternateStatusCode">The alternate <see cref="HttpStatusCode"/> where result is <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the request value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="IActionResult"/> (either <see cref="ValueContentResult"/> on non-<c>null</c> result; otherwise, a <see cref="StatusCodeResult"/>).</returns>
        public Task<IActionResult> PutWithResultAsync<TValue, TResult>(HttpRequest request, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
            => PutWithResultInternalAsync(request, true, value, function, statusCode, alternateStatusCode, operationType, valueIsRequired, validator, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> and a response of <see cref="Type"/> <typeparamref name="TResult"/> (with a <see cref="Result{T}"/>).
        /// </summary>
        public async Task<IActionResult> PutWithResultInternalAsync<TValue, TResult>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TResult>>> function, HttpStatusCode statusCode = HttpStatusCode.OK,
            HttpStatusCode alternateStatusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Update, bool valueIsRequired = true, IValidator<TValue>? validator = null, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPut(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Put}' to use {nameof(PutWithResultAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, valueIsRequired, validator, ct).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                var result = await function(wapv!, ct).ConfigureAwait(false);
                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false)
                    : ValueContentResult.CreateResult(result.Value, statusCode, alternateStatusCode, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutWithResultAsync<TValue>(HttpRequest request, Func<WebApiParam, Task<Result<TValue>>> get, Func<WebApiParam<TValue>, Task<Result<TValue>>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false) where TValue : class
            => PutWithResultAsync(request, (p, _) => get(p), (p, _) => put(p), statusCode, operationType, validator, simulatedConcurrency, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutWithResultAsync<TValue>(HttpRequest request, Func<WebApiParam, CancellationToken, Task<Result<TValue>>> get, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TValue>>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false, CancellationToken cancellationToken = default) where TValue : class
            => PutWithResultInternalAsync(request, false, default!, get, put, statusCode, operationType, validator, simulatedConcurrency, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutWithResultAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam, Task<Result<TValue>>> get, Func<WebApiParam<TValue>, Task<Result<TValue>>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false) where TValue : class
            => PutWithResultAsync(request, value, (p, _) => get(p), (p, _) => put(p), statusCode, operationType, validator, simulatedConcurrency, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="value">The value (already deserialized).</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> PutWithResultAsync<TValue>(HttpRequest request, TValue value, Func<WebApiParam, CancellationToken, Task<Result<TValue>>> get, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TValue>>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false, CancellationToken cancellationToken = default) where TValue : class
            => PutWithResultInternalAsync(request, true, value, get, put, statusCode, operationType, validator, simulatedConcurrency, cancellationToken);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Put"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value (with a <see cref="Result{T}"/>).
        /// </summary>
        private async Task<IActionResult> PutWithResultInternalAsync<TValue>(HttpRequest request, bool useValue, TValue value, Func<WebApiParam, CancellationToken, Task<Result<TValue>>> get, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TValue>>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false, CancellationToken cancellationToken = default) where TValue : class
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPut(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Put}' to use {nameof(PutWithResultAsync)}.", nameof(request));

            if (get == null)
                throw new ArgumentNullException(nameof(get));

            if (put == null)
                throw new ArgumentNullException(nameof(put));

            return await RunAsync(request, async (wap, ct) =>
            {
                var (wapv, vex) = await ValidateValueAsync(wap, useValue, value, true, validator, ct).ConfigureAwait(false);
                if (vex != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, vex, Settings, Logger, OnUnhandledExceptionAsync, cancellationToken).ConfigureAwait(false);

                // Get the current value before we perform the update; also performing a concurrency match.
                var cresult = await get(wap, ct).ConfigureAwait(false);
                var ex = cresult.IsFailure ? cresult.Error : (cresult.Value == null ? new NotFoundException() : ConcurrencyETagMatching(wap, cresult.Value, wapv!.Value, simulatedConcurrency));

                if (ex is not null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, ex, Settings, Logger, OnUnhandledExceptionAsync, ct).ConfigureAwait(false);

                // Update the value.
                var result = await put(wapv!, ct).ConfigureAwait(false);
                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false)
                    : ValueContentResult.CreateResult(result.Value, statusCode, null, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region DeleteWithResultAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Delete"/> operation (with a <see cref="Result"/>).
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public Task<IActionResult> DeleteWithResultAsync(HttpRequest request, Func<WebApiParam, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Delete)
            => DeleteWithResultAsync(request, (p, _) => function(p), statusCode, operationType, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Delete"/> operation (with a <see cref="Result"/>).
        /// </summary>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="function">The function to execute.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where result is not <c>null</c>.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        public async Task<IActionResult> DeleteWithResultAsync(HttpRequest request, Func<WebApiParam, CancellationToken, Task<Result>> function, HttpStatusCode statusCode = HttpStatusCode.NoContent, OperationType operationType = OperationType.Delete, CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsDelete(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Delete}' to use {nameof(DeleteWithResultAsync)}.", nameof(request));

            if (function == null)
                throw new ArgumentNullException(nameof(function));

            return await RunAsync(request, async (wap, ct) =>
            {
                var result = await function(wap, ct).ConfigureAwait(false);
                if (result.IsFailure)
                {
                    // Return default status code where configured for a NotFoundException. 
                    if (!(ConvertNotfoundToDefaultStatusCodeOnDelete && (result.Error is NotFoundException || (result.Error is AggregateException ae && ae.InnerException is NotFoundException))))
                        return await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, OnUnhandledExceptionAsync, ct).ConfigureAwait(false);
                }

                return new ExtendedStatusCodeResult(statusCode);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion

        #region PatchWithResultAsync

        /// <summary>
        /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value to patch into. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the patched value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>Currently on the <see cref="JsonMergePatch"/> is supported.</remarks>
        public Task<IActionResult> PatchWithResultAsync<TValue>(HttpRequest request, Func<WebApiParam, Task<Result<TValue>>> get, Func<WebApiParam<TValue>, Task<Result<TValue>>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false) where TValue : class
            => PatchWithResultAsync(request, (p, _) => get(p), (p, _) => put(p), statusCode, operationType, validator, simulatedConcurrency, CancellationToken.None);

        /// <summary>
        /// Performs a <see cref="HttpMethods.Patch"/> operation with a request JSON content value of <see cref="Type"/> <typeparamref name="TValue"/> returning a corresponding response value (with a <see cref="Result{T}"/>).
        /// </summary>
        /// <typeparam name="TValue">The request JSON content value <see cref="Type"/>.</typeparam>
        /// <param name="request">The <see cref="HttpRequest"/>.</param>
        /// <param name="get">The function to execute the <i>get</i> to retrieve the value to patch into. Where this returns a <c>null</c> then this will result in a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/>.</param>
        /// <param name="put">The function to execute the <i>put</i> to replace (update) the patched value.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/> where successful.</param>
        /// <param name="operationType">The <see cref="OperationType"/>.</param>
        /// <param name="validator">The <see cref="IValidator{T}"/> to validate the deserialized value.</param>
        /// <param name="simulatedConcurrency">Indicates whether simulated concurrency (ETag) checking/generation is performed as underlying data source does not support.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="ExtendedStatusCodeResult"/> <see cref="IActionResult"/> where successful.</returns>
        /// <remarks>Currently on the <see cref="JsonMergePatch"/> is supported.</remarks>
        public async Task<IActionResult> PatchWithResultAsync<TValue>(HttpRequest request, Func<WebApiParam, CancellationToken, Task<Result<TValue>>> get, Func<WebApiParam<TValue>, CancellationToken, Task<Result<TValue>>> put, HttpStatusCode statusCode = HttpStatusCode.OK,
            OperationType operationType = OperationType.Update, IValidator<TValue>? validator = null, bool simulatedConcurrency = false, CancellationToken cancellationToken = default) where TValue : class
        {
            if (JsonMergePatch == null)
                throw new InvalidOperationException($"To use the '{nameof(PatchWithResultAsync)}' methods the '{nameof(JsonMergePatch)}' object must be passed in the constructor. Where using dependency injection consider using '{nameof(Microsoft.Extensions.DependencyInjection.IServiceCollectionExtensions.AddJsonMergePatch)}' to add and configure the supported options.");

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (!HttpMethods.IsPatch(request.Method))
                throw new ArgumentException($"HttpRequest.Method is '{request.Method}'; must be '{HttpMethods.Patch}' to use {nameof(PatchWithResultAsync)}.", nameof(request));

            if (get == null)
                throw new ArgumentNullException(nameof(get));

            if (put == null)
                throw new ArgumentNullException(nameof(put));

            return await RunAsync(request, async (wap, ct) =>
            {
                // Make sure that the only the support content types are used.
                var hct = request.GetTypedHeaders()?.ContentType?.MediaType.Value;
                if (StringComparer.OrdinalIgnoreCase.Compare(hct, HttpConsts.MergePatchMediaTypeName) != 0 && StringComparer.OrdinalIgnoreCase.Compare(hct, MediaTypeNames.Application.Json) != 0)
                    return new ContentResult
                    {
                        StatusCode = (int)HttpStatusCode.UnsupportedMediaType,
                        ContentType = MediaTypeNames.Text.Plain,
                        Content = $"Unsupported 'Content-Type' for a PATCH; only JSON Merge Patch is supported using either: 'application/merge-patch+json' or '{MediaTypeNames.Application.Json}'."
                    };

                // Retrieve the JSON content string; there must be some content of some type.
                var json = await request.ReadAsStringAsync(true, ct).ConfigureAwait(false);
                if (json.Exception != null)
                    return await CreateActionResultFromExceptionAsync(this, request.HttpContext, json.Exception, Settings, Logger, UnhandledExceptionAsync, ct).ConfigureAwait(false);

                // Note: the JsonMergePatch will throw JsonMergePatchException on error which will be automatically converted to an appropriate IActionResult by the invoking RunAsync method.
                var mresult = await JsonMergePatch.MergeWithResultAsync<TValue>(json.Content!, async (jpv, ct2) =>
                {
                    // Get the current value and perform a concurrency match before we perform the merge.
                    var rv = await get(wap, ct2).ConfigureAwait(false);
                    var ex = rv.IsFailure ? rv.Error : (rv.Value is null ? new NotFoundException() : ConcurrencyETagMatching(wap, rv.Value, jpv, simulatedConcurrency));
                    return ex is null ? Result.Ok(rv.Value!) : Result.Fail(ex);
                }, ct).ConfigureAwait(false);

                // Only invoke the put function where something was *actually* changed.
                var result = await mresult.ThenAsync(async v =>
                {
                    if (v.HasChanges)
                    {
                        if (validator != null)
                        {
                            var vr = await validator.ValidateAsync(v.Value!, ct).ConfigureAwait(false);
                            if (vr.HasErrors)
                                return vr.ToValidationException()!;
                        }

                        return await put(new WebApiParam<TValue>(wap, v.Value!), ct).ConfigureAwait(false);
                    }

                    return Result.Ok(v.Value!);
                }).ConfigureAwait(false);

                return result.IsFailure
                    ? await CreateActionResultFromExceptionAsync(this, request.HttpContext, result.Error, Settings, Logger, UnhandledExceptionAsync, cancellationToken).ConfigureAwait(false)
                    : ValueContentResult.CreateResult(result.Value, statusCode, null, JsonSerializer, wap.RequestOptions, checkForNotModified: false, location: null);
            }, operationType, cancellationToken).ConfigureAwait(false);
        }

        #endregion
    }
}
