// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a typed <see cref="HttpClient"/> base wrapper.
    /// </summary>
    /// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
    public abstract class TypedHttpClientBase<TSelf> : TypedHttpClientBase where TSelf : TypedHttpClientBase<TSelf>
    {
        private static readonly Random _random = new(); // Used to add jitter (random 0-500ms) per retry.

        private int? _retryCount;
        private double? _retrySeconds;
        private bool _ensureSuccess = false;
        private bool _throwTransientException;
        private bool _throwKnownException;
        private bool _throwKnownUseContentAsMessage;
        private PolicyBuilder<HttpResponseMessage> _retryPolicy;
        private Func<HttpResponseMessage?, Exception?, bool> _isTransient = IsTransient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientBase{TBase}"/>.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public TypedHttpClientBase(HttpClient client, IJsonSerializer jsonSerializer, ExecutionContext executionContext, SettingsBase settings, ILogger<TypedHttpClientBase<TSelf>> logger) : base(client, jsonSerializer)
        {
            ExecutionContext = executionContext ?? throw new ArgumentNullException(nameof(executionContext));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            RequestLogger = HttpRequestLogger.Create(settings, logger);
            _retryPolicy = HttpPolicyExtensions.HandleTransientHttpError();
        }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        protected ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        protected SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the <see cref="HttpRequestLogger"/>.
        /// </summary>
        protected HttpRequestLogger RequestLogger { get; }

        /// <summary>
        /// Gets the list of correlation header names.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.CorrelationIdHeaderName"/> and '<c>x-ms-client-tracking-id</c>'.</remarks>
        protected virtual IEnumerable<string> CorrelationHeaderNames => new string[] { HttpConsts.CorrelationIdHeaderName, "x-ms-client-tracking-id" };

        /// <summary>
        /// Gets or sets the underlying retry policy for the <see cref="WithRetry(int?, double?)"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpPolicyExtensions.HandleTransientHttpError"/>.</remarks>
        public PolicyBuilder<HttpResponseMessage> RetryPolicy { get => _retryPolicy; set => _retryPolicy = value ?? throw new ArgumentNullException(nameof(RetryPolicy)); }

        /// <summary>
        /// Indicates whether to check the <see cref="HttpResponseMessage"/> and where considered a transient error then a <see cref="TransientException"/> will be thrown.
        /// </summary>
        /// <param name="predicate">An oprtional predicate to determine whether the error is considered transient. Defaults to <see cref="TypedHttpClientBase.IsTransient(HttpResponseMessage?, Exception?)"/> where not specified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This occurs outside of any <see cref="WithRetry(int?, double?)"/>.<para>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</para></remarks>
        public TSelf ThrowTransientException(Func<HttpResponseMessage?, Exception?, bool>? predicate = null)
        {
            _throwTransientException = true;
            _isTransient = predicate ?? IsTransient;
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be thrown.
        /// </summary>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This occurs outside of any <see cref="WithRetry(int?, double?)"/>.<para>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</para></remarks>
        public TSelf ThrowKnownException(bool useContentAsErrorMessage = false)
        {
            _throwKnownException = true;
            _throwKnownUseContentAsMessage = useContentAsErrorMessage;
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to perform a retry where an underlying transient error occurs; see <see cref="RetryPolicy"/>.
        /// </summary>
        /// <param name="count">The number of times to retry. Defaults to <see cref="SettingsBase.HttpRetryCount"/>.</param>
        /// <param name="seconds">The base number of seconds to delay between retries. Defaults to <see cref="SettingsBase.HttpRetrySeconds"/></param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TSelf WithRetry(int? count = null, double? seconds = null)
        { 
            var retryCount = Settings.HttpRetryCount;
            var retrySeconds = Settings.HttpRetrySeconds;

            _retryCount = count ?? retryCount;
            if (_retryCount < 0)
                _retryCount = retryCount;

            _retrySeconds = seconds ?? retrySeconds;
            if (_retrySeconds < retrySeconds)
                _retrySeconds = retrySeconds;

            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to automatically perform an <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TSelf EnsureSuccess()
        {
            _ensureSuccess = true;
            return (TSelf)this;
        }

        /// <summary>
        /// Resets the <see cref="TypedHttpClientBase{TSelf}"/> to its initial state.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        public virtual TSelf Reset()
        {
            _retryCount = null;
            _retrySeconds = null;
            _throwTransientException = false;
            _throwKnownException = false;
            _throwKnownUseContentAsMessage = false;
            _isTransient = IsTransient;
            _ensureSuccess = false;

            return (TSelf)this;
        }

        #region SendAsync

        /// <summary>
        /// Sends the <paramref name="request"/> returning the <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;

            try
            {
                foreach (var name in CorrelationHeaderNames)
                {
                    request.Headers.Add(name, ExecutionContext.CorrelationId);
                }

                await RequestLogger.LogRequestAsync(request).ConfigureAwait(false);

                var req = request;
                response = await
                    (_retryPolicy ?? HttpPolicyExtensions.HandleTransientHttpError())
                    .WaitAndRetryAsync(_retryCount ?? 0, attempt => TimeSpan.FromSeconds(Math.Pow(_retrySeconds ?? 0, attempt)).Add(TimeSpan.FromMilliseconds(_random.Next(0, 500))), async (result, timeSpan, retryCount, context) =>
                    {
                        if (result.Exception == null)
                            Logger.LogWarning("Request failed with {HttpStatusCodeText} ({HttpStatusCode}). Waiting {HttpRetryTimeSpan} before next retry. Retry attempt {HttpRetryCount}.",
                                result.Result.StatusCode, (int)result.Result.StatusCode, timeSpan, retryCount);
                        else
                            Logger.LogWarning(result.Exception, "Request failed with '{ErrorMessage}' Waiting {HttpRetryTimeSpan} before next retry. Retry attempt {HttpRetryCount}.",
                                result.Exception.Message, timeSpan, retryCount);

                        // Clone and dispose of existing request to avoid error: The request message was already sent. Cannot send the same request message multiple times.
                        var tmp = await CloneAsync(req).ConfigureAwait(false);
                        req.Dispose();
                        req = tmp;
                    })
                    .ExecuteAsync(() => Client.SendAsync(req, cancellationToken));

                await RequestLogger.LogResponseAsync(request, response).ConfigureAwait(false);
            }
            catch (HttpRequestException hrex)
            {
                if (_throwTransientException && _isTransient(null, hrex))
                    throw new TransientException(null, hrex);

                throw;
            }

            // This is the result of the final request after leaving the retry policy logic.
            if (_throwTransientException && _isTransient(response, null))
                throw new TransientException();

            if (_throwKnownException)
            {
                var eex = await response.ToExtendedExceptionAsync(_throwKnownUseContentAsMessage).ConfigureAwait(false);
                if (eex != null)
                    throw (Exception)eex;
            }

            if (_ensureSuccess)
                response.EnsureSuccessStatusCode();

            return response;
        }

        /// <summary>
        /// Clones the <see cref="HttpRequestMessage"/>; inspired by <see href="https://stackoverflow.com/questions/18000583/re-send-httprequestmessage-exception"/>.
        /// </summary>
        private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request)
        {
            var clone = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = await CloneAsync(request.Content).ConfigureAwait(false),
                Version = request.Version
            };

            foreach (KeyValuePair<string, object?> prop in request.Properties)
            {
                clone.Properties.Add(prop.Key, prop.Value);
            }

            foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers)
            {
                clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            return clone;
        }

        /// <summary>
        /// Clones the <see cref="HttpContent"/>.
        /// </summary>
        private static async Task<HttpContent?> CloneAsync(HttpContent? content)
        {
            if (content == null)
                return null;

            var ms = new MemoryStream();
            await content.CopyToAsync(ms).ConfigureAwait(false);
            ms.Position = 0;

            var clone = new StreamContent(ms);
            foreach (KeyValuePair<string, IEnumerable<string>> header in content.Headers)
            {
                clone.Headers.Add(header.Key, header.Value);
            }

            return clone;
        }

        #endregion

        #region HeadAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Head"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public async Task<HttpResult> HeadAsync(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Head, requestUri, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        #endregion

        #region GetAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected async Task<HttpResult> GetAsync(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation and deserialize the JSON <see cref="HttpResponseMessage.Content"/> to the specified .NET object <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> GetAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateRequestAsync(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation and deserialize the JSON <see cref="HttpResponseMessage.Content"/> to the specified .NET object <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TCollResult">The <see cref="ICollectionResult{TColl, TItem}"/> response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
        /// <typeparam name="TItem">The item <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TCollResult>> GetCollectionResultAsync<TCollResult, TColl, TItem>(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
            where TCollResult : ICollectionResult<TColl, TItem>, new()
            where TColl : ICollection<TItem>
        {
            var response = await SendAsync(await CreateRequestAsync(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TCollResult, TColl, TItem>(response, JsonSerializer).ConfigureAwait(false);
        }

        #endregion

        #region PostAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected async Task<HttpResult> PostAsync(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Post, requestUri, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected async Task<HttpResult> PostAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateContentRequestAsync(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected async Task<HttpResult> PostAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = (value is HttpContent content)
                ? await SendAsync(await CreateContentRequestAsync(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)
                : await SendAsync(await CreateJsonRequestAsync(HttpMethod.Post, requestUri, value, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

            return await HttpResult.CreateAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateRequestAsync(HttpMethod.Post, requestUri, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateContentRequestAsync(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateJsonRequestAsync(HttpMethod.Post, requestUri, value, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer);
        }

        #endregion

        #region PutAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected async Task<HttpResult> PutAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateContentRequestAsync(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult> PutAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = (value is HttpContent content)
                ? await SendAsync(await CreateContentRequestAsync(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)
                : await SendAsync(await CreateJsonRequestAsync(HttpMethod.Put, requestUri, value, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

            return await HttpResult.CreateAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PutAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateContentRequestAsync(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateJsonRequestAsync(HttpMethod.Put, requestUri, value, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer);
        }

        #endregion

        #region PatchAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected async Task<HttpResult> PatchAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateContentRequestAsync(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="json"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="patchOption">The <see cref="HttpPatchOption"/>.</param>
        /// <param name="json">The JSON formatted as per the selected <paramref name="patchOption"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult> PatchAsync(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            if (patchOption == HttpPatchOption.NotSpecified)
                throw new ArgumentException("A valid patch option must be specified.", nameof(patchOption));

            var content = new StringContent(json, Encoding.UTF8, patchOption == HttpPatchOption.JsonPatch ? HttpConsts.JsonPatchMediaTypeName : HttpConsts.MergePatchMediaTypeName);
            return await HttpResult.CreateAsync(await SendAsync(await CreateContentRequestAsync(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateContentRequestAsync(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="json"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="patchOption">The <see cref="HttpPatchOption"/>.</param>
        /// <param name="json">The JSON formatted as per the selected <paramref name="patchOption"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
        {
            if (patchOption == HttpPatchOption.NotSpecified)
                throw new ArgumentException("A valid patch option must be specified.", nameof(patchOption));

            var content = new StringContent(json, Encoding.UTF8, patchOption == HttpPatchOption.JsonPatch ? HttpConsts.JsonPatchMediaTypeName : HttpConsts.MergePatchMediaTypeName);
            var response = await SendAsync(await CreateContentRequestAsync(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer).ConfigureAwait(false);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Send a <see cref="HttpMethod.Delete"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected async Task<HttpResult> DeleteAsync(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<IHttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Delete, requestUri, requestOptions, args?.ToArray()!).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        #endregion
    }
}