// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
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

            foreach (KeyValuePair<string, object> prop in request.Properties)
            {
                clone.Properties.Add(prop);
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
    }
}