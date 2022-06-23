// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

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
        private List<HttpStatusCode>? _ensureStatusCodes;
        private bool _throwTransientException;
        private bool _throwKnownException;
        private bool _throwKnownUseContentAsMessage;
        private PolicyBuilder<HttpResponseMessage>? _retryPolicy;
        private Func<HttpResponseMessage?, Exception?, (bool result, string error)> _isTransient = IsTransient;
        private TimeSpan? _timeout;
        private TimeSpan? _maxRetryDelay;

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
        /// Sets the underlying retry policy for the <see cref="WithRetry(int?, double?)"/>.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpPolicyExtensions.HandleTransientHttpError"/> with additional handling of <see cref="SocketException"/> and <see cref="TimeoutException"/>.</remarks>
        public TSelf WithCustomRetryPolicy(PolicyBuilder<HttpResponseMessage> retryPolicy)
        {
            _retryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to check the <see cref="HttpResponseMessage"/> and where considered a transient error then a <see cref="TransientException"/> will be thrown.
        /// </summary>
        /// <param name="predicate">An optional predicate to determine whether the error is considered transient. Defaults to <see cref="TypedHttpClientBase.IsTransient(HttpResponseMessage?, Exception?)"/> where not specified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This occurs outside of any <see cref="WithRetry(int?, double?)"/>.<para>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</para></remarks>
        public TSelf ThrowTransientException(Func<HttpResponseMessage?, Exception?, (bool result, string error)>? predicate = null)
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
        /// Indicates whether to perform a retry where an underlying transient error occurs.
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
        /// Adds the <see cref="HttpStatusCode.OK"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureOK() => Ensure(HttpStatusCode.OK);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.NoContent"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureNoContent() => Ensure(HttpStatusCode.NoContent);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.Accepted"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureAccepted() => Ensure(HttpStatusCode.Accepted);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.Created"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureCreated() => Ensure(HttpStatusCode.Created);

        /// <summary>
        /// Adds the <paramref name="statusCodes"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <param name="statusCodes">One or more <see cref="HttpStatusCode">status codes</see> to be verified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation (see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf Ensure(params HttpStatusCode[] statusCodes)
        {
            if (statusCodes != null && statusCodes.Length > 0)
            {
                if (_ensureStatusCodes == null)
                    _ensureStatusCodes = new List<HttpStatusCode>(statusCodes);
                else
                    _ensureStatusCodes.AddRange(statusCodes);
            }

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
            _ensureStatusCodes = null;
            _timeout = null;
            _maxRetryDelay = null;

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
            CancellationTokenSource? cts = null;

            try
            {
                try
                {
                    var sw = Stopwatch.StartNew();
                    CorrelationHeaderNames.ForEach(n => request.Headers.TryAddWithoutValidation(n, ExecutionContext.CorrelationId));

                    await OnBeforeRequest(request, cancellationToken).ConfigureAwait(false);
                    await RequestLogger.LogRequestAsync(request, cancellationToken).ConfigureAwait(false);

                    var req = request;
                    response = await (_retryPolicy ?? HttpPolicyExtensions.HandleTransientHttpError().Or<SocketException>().Or<TimeoutException>().OrResult(msg => msg.StatusCode == HttpStatusCode.TooManyRequests))
                        .WaitAndRetryAsync(_retryCount ?? 0,
                        sleepDurationProvider: (attempt, e, Context) =>
                        {
                            TimeSpan? delay = null;
                            if (e.Result?.Headers.RetryAfter?.Delta != null)
                                delay = e.Result.Headers.RetryAfter.Delta.Value;

                            if (e.Result?.Headers.RetryAfter?.Date != null)
                                delay = e.Result.Headers.RetryAfter.Date.Value - DateTimeOffset.UtcNow;

                            // Calculate exponential with jitter.
                            delay ??= TimeSpan.FromSeconds(Math.Pow(_retrySeconds ?? 0, attempt)).Add(TimeSpan.FromMilliseconds(_random.Next(0, 500)));

                            // Do not go over max delay.
                            var maxDelay = _maxRetryDelay ?? Settings.MaxRetryDelay;
                            return delay.Value > maxDelay ? maxDelay : delay.Value;
                        },
                        onRetryAsync: async (result, timeSpan, retryCount, context) =>
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
                            sw.Reset();
                        })
                        .ExecuteAsync(async () =>
                        {
                            try
                            {
                                return await Client.SendAsync(req, SetCancellationBasedOnTimeout(cancellationToken, out cts)).ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                                      when (!cancellationToken.IsCancellationRequested)
                            {
                                throw new TimeoutException();
                            }
                        }).ConfigureAwait(false); ;

                    await RequestLogger.LogResponseAsync(request, response, sw.Elapsed, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is TimeoutException || ex is SocketException)
                {
                    // both TimeoutException and SocketException are transient and indicate a connection was terminated
                    throw new TransientException("Timeout when calling service", ex);
                }
                catch (HttpRequestException hrex)
                {
                    (bool isTransient, string error) = IsTransient(null, hrex);
                    if (_throwTransientException && isTransient)
                        throw new TransientException(error, hrex);

                    throw;
                }
                finally
                {
                    cts?.Dispose();
                }

                // This is the result of the final request after leaving the retry policy logic.
                (bool wasTransient, string errorMsg) = IsTransient(response, null);
                if (_throwTransientException && wasTransient)
                    throw new TransientException(errorMsg);

                if (_throwKnownException)
                {
                    var eex = await response.ToExtendedExceptionAsync(_throwKnownUseContentAsMessage, cancellationToken).ConfigureAwait(false);
                    if (eex != null)
                        throw (Exception)eex;
                }

                if (_ensureSuccess)
                    response.EnsureSuccessStatusCode();

                if (_ensureStatusCodes != null && !_ensureStatusCodes.Contains(response.StatusCode))
                    throw new HttpRequestException($"Response status code {response.StatusCode}; expected one of the following: {string.Join(", ", _ensureStatusCodes)}.");

                return response;
            }
            finally
            {
                Reset();
            }
        }

        /// <summary>
        /// Sets timeout for given request
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        public TSelf WithTimeout(TimeSpan timeout)
        {
            _timeout = timeout;
            return (TSelf)this;
        }

        /// <summary>
        /// Sets max retry delay that polly retries will be capped with (this affects mostly 429 and 503 responses that can return Retry-After header).
        /// Default is 30s but it can be overridden for async calls (e.g. when using service bus trigger).
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        public TSelf WithMaxRetryDelay(TimeSpan maxRetryDelay)
        {
            _maxRetryDelay = maxRetryDelay;
            return (TSelf)this;
        }

        private CancellationToken SetCancellationBasedOnTimeout(CancellationToken cancellationToken, out CancellationTokenSource? cts)
        {
            var timeout = _timeout ?? TimeSpan.FromSeconds(Settings.HttpTimeoutSeconds);
            if (timeout == Timeout.InfiniteTimeSpan)
            {
                // No need to create a CTS if there's no timeout
                cts = null;
                return cancellationToken;
            }
            else
            {
                cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);
                return cts.Token;
            }
        }

        /// <summary>
        /// Provides an opportunity to update the <paramref name="request"/> before sending.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        protected virtual Task OnBeforeRequest(HttpRequestMessage request, CancellationToken cancellationToken) => Task.CompletedTask;

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
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Head, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

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
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

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
            var response = await SendAsync(await CreateRequestAsync(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken).ConfigureAwait(false);
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
            var response = await SendAsync(await CreateRequestAsync(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TCollResult, TColl, TItem>(response, JsonSerializer, cancellationToken).ConfigureAwait(false);
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
        protected async Task<HttpResult> PostAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Post, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected async Task<HttpResult> PostAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateContentRequestAsync(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

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
        protected async Task<HttpResult> PostAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            var response = (value is HttpContent content)
                ? await SendAsync(await CreateContentRequestAsync(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)
                : await SendAsync(await CreateJsonRequestAsync(HttpMethod.Post, requestUri, value, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

            return await HttpResult.CreateAsync(response, cancellationToken).ConfigureAwait(false);
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
        protected async Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateRequestAsync(HttpMethod.Post, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken);
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
        protected async Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args =  null, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateContentRequestAsync(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken);
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
        protected async Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args =  null, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateJsonRequestAsync(HttpMethod.Post, requestUri, value, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken);
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
        protected async Task<HttpResult> PutAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateContentRequestAsync(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

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
        protected async Task<HttpResult> PutAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            var response = (value is HttpContent content)
                ? await SendAsync(await CreateContentRequestAsync(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false)
                : await SendAsync(await CreateJsonRequestAsync(HttpMethod.Put, requestUri, value, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

            return await HttpResult.CreateAsync(response, cancellationToken).ConfigureAwait(false);
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
        protected async Task<HttpResult<TResponse>> PutAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateContentRequestAsync(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken);
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
        protected async Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateJsonRequestAsync(HttpMethod.Put, requestUri, value, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken);
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
        protected async Task<HttpResult> PatchAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateContentRequestAsync(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

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
        protected async Task<HttpResult> PatchAsync(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            if (patchOption == HttpPatchOption.NotSpecified)
                throw new ArgumentException("A valid patch option must be specified.", nameof(patchOption));

            var content = new StringContent(json, Encoding.UTF8, patchOption == HttpPatchOption.JsonPatch ? HttpConsts.JsonPatchMediaTypeName : HttpConsts.MergePatchMediaTypeName);
            return await HttpResult.CreateAsync(await SendAsync(await CreateContentRequestAsync(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
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
        protected async Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(await CreateContentRequestAsync(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken).ConfigureAwait(false);
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
        protected async Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
        {
            if (patchOption == HttpPatchOption.NotSpecified)
                throw new ArgumentException("A valid patch option must be specified.", nameof(patchOption));

            var content = new StringContent(json, Encoding.UTF8, patchOption == HttpPatchOption.JsonPatch ? HttpConsts.JsonPatchMediaTypeName : HttpConsts.MergePatchMediaTypeName);
            var response = await SendAsync(await CreateContentRequestAsync(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken).ConfigureAwait(false);
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
        protected async Task<HttpResult> DeleteAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Delete, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

        #endregion

        #region Healthcheck

        /// <summary>
        /// Performs a health check by sending a <see cref="HttpMethod.Head"/> request to base Uri as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public virtual async Task<HttpResult> HealthCheckAsync(CancellationToken cancellationToken = default)
         => await HeadAsync(string.Empty, null, null, cancellationToken).ConfigureAwait(false);

        #endregion
    }
}