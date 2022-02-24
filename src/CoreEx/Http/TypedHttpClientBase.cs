// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a typed <see cref="HttpClient"/> base wrapper.
    /// </summary>
    /// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
    public abstract class TypedHttpClientBase<TSelf> where TSelf : TypedHttpClientBase<TSelf>
    {
        private static readonly Random _random = new(); // Used to add jitter (random 0-500ms) per retry.

        private int? _retryCount;
        private double? _retrySeconds;
        private bool _ensureSuccess = false;
        private bool _throwTransientException;
        private bool _throwValidationException;
        private string? _throwValidationMessage;
        private PolicyBuilder<HttpResponseMessage>? _retryPolicy;
        private Func<HttpResponseMessage?, Exception?, bool> _isTransient = IsTransient;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientBase{TBase}"/>.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public TypedHttpClientBase(HttpClient client, IJsonSerializer jsonSerializer, SettingsBase settings, ILogger<TypedHttpClientBase<TSelf>> logger)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
            JsonSerializer = jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Logger = logger ?? throw new ArgumentNullException(nameof(logger));
            RequestLogger = HttpRequestLogger.Create(settings, logger);
        }

        /// <summary>
        /// Gets the underlying <see cref="HttpClient"/>.
        /// </summary>
        protected HttpClient Client { get; }

        /// <summary>
        /// Gets the <see cref="IJsonSerializer"/>.
        /// </summary>
        protected IJsonSerializer JsonSerializer { get; }

        /// <summary>
        /// Gets the <see cref="SettingsBase"/>.
        /// </summary>
        protected SettingsBase Settings { get; }

        /// <summary>
        /// Gets the <see cref="ILogger"/>.
        /// </summary>
        protected ILogger Logger { get; }

        /// <summary>
        /// Gets the list of correlation header names.
        /// </summary>
        protected virtual IEnumerable<string> CorrelationHeaderNames => new string[] { "x-correlation-id", "x-ms-client-tracking-id" };

        /// <summary>
        /// Gets the <see cref="HttpRequestLogger"/>.
        /// </summary>
        protected HttpRequestLogger RequestLogger { get; }

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> with no content.
        /// </summary>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected HttpRequestMessage CreateRequest(HttpMethod method, string requestUri) => new(method, requestUri);

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected HttpRequestMessage CreateRequest(HttpMethod method, string requestUri, HttpContent content)
        {
            var request = CreateRequest(method, requestUri);
            request.Content = content;
            return request;
        }

        /// <summary>
        /// Create an <see cref="HttpRequestMessage"/> serializing the <paramref name="value"/> as JSON content.
        /// </summary>
        /// <typeparam name="TReq">The request <see cref="Type"/>.</typeparam>
        /// <param name="method">The <see cref="HttpMethod"/>.</param>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value to be serialized to JSON.</param>
        /// <returns>The <see cref="HttpRequestMessage"/>.</returns>
        protected HttpRequestMessage CreateJsonRequest<TReq>(HttpMethod method, string requestUri, TReq value)
            => CreateRequest(method, requestUri, new StringContent(JsonSerializer.Serialize(value), Encoding.UTF8, MediaTypeNames.Application.Json));

        /// <summary>
        /// Deserialize the JSON <see cref="HttpResponseMessage.Content"/> into <see cref="Type"/> of <typeparamref name="TResp"/>.
        /// </summary>
        /// <typeparam name="TResp">The response <see cref="Type"/>.</typeparam>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <returns>The deserialized response value.</returns>
        protected async Task<TResp> ReadAsJsonAsync<TResp>(HttpResponseMessage response)
        {
            response.EnsureSuccessStatusCode();
            if (response.Content == null)
                return default!;

            var str = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonSerializer.Deserialize<TResp>(str)!;
        }

        /// <summary>
        /// Sends the <paramref name="request"/> returning the <see cref="HttpResponseMessage"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        protected virtual async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                foreach (var name in CorrelationHeaderNames)
                {
                    request.Headers.Add(name, Executor.GetCorrelationId());
                }

                await RequestLogger.LogRequestAsync(request).ConfigureAwait(false);

                var req = request;
                var response = await
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

                // This is the result of the final request after leaving the retry policy logic.
                if (_throwTransientException && _isTransient(response, null))
                    throw new TransientException();

                if (_throwValidationException && response.StatusCode == HttpStatusCode.BadRequest)
                    throw new ValidationException(_throwValidationMessage ?? await response.Content.ReadAsStringAsync().ConfigureAwait(false));

                if (_ensureSuccess)
                    response.EnsureSuccessStatusCode();

                return response;
            }
            catch (HttpRequestException hrex)
            {
                if (_throwTransientException && _isTransient(null, hrex))
                    throw new TransientException(null, hrex);

                throw;
            }
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
        /// <param name="predicate">An oprtional predicate to determine whether the error is considered transient. Defaults to <see cref="IsTransient(HttpResponseMessage?, Exception?)"/> where not specified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This occurs outside of any <see cref="WithRetry(int?, double?)"/> or <see cref="WithRetry(PolicyBuilder{HttpResponseMessage}, int?, double?)"/>.</remarks>
        public TSelf ThrowTransientException(Func<HttpResponseMessage?, Exception?, bool>? predicate)
        {
            _throwTransientException = true;
            _isTransient = predicate ?? IsTransient;
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where <see cref="HttpStatusCode.BadRequest"/> throw a <see cref="ValidationException"/>.
        /// </summary>
        /// <param name="message">The message override; otherwise, by default will use the <see cref="HttpResponseMessage.Content"/>.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This occurs outside of any <see cref="WithRetry(int?, double?)"/> or <see cref="WithRetry(PolicyBuilder{HttpResponseMessage}, int?, double?)"/>.</remarks>
        public TSelf ThrowValidationException(string? message = null)
        {
            _throwValidationException = true;
            _throwValidationMessage = message;
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to perform a retry where an underlying transient error occurs using the default policy (<see cref="HttpPolicyExtensions.HandleTransientHttpError"/>).
        /// </summary>
        /// <param name="count">The number of times to retry. Defaults to <see cref="SettingsBase.HttpRetryCount"/>.</param>
        /// <param name="seconds">The base number of seconds to delay between retries. Defaults to <see cref="SettingsBase.HttpRetrySeconds"/></param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        public TSelf WithRetry(int? count = null, double? seconds = null) => WithRetry(HttpPolicyExtensions.HandleTransientHttpError(), count, seconds);

        /// <summary>
        /// Indicates whether to perform a retry where an underlying transient error occurs using an alternate <see cref="PolicyBuilder"/>.
        /// </summary>
        /// <param name="policyBuilder">The alternate <see cref="PolicyBuilder"/>.</param>
        /// <param name="count">The number of times to retry. Defaults to <see cref="SettingsBase.HttpRetryCount"/>.</param>
        /// <param name="seconds">The base number of seconds to delay between retries. Defaults to <see cref="SettingsBase.HttpRetrySeconds"/></param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        public TSelf WithRetry(PolicyBuilder<HttpResponseMessage> policyBuilder, int? count = null, double? seconds = null)
        {
            _retryPolicy = policyBuilder ?? throw new ArgumentNullException(nameof(policyBuilder));

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
            _throwValidationException = false;
            _throwValidationMessage = null;
            _retryPolicy = null;
            _isTransient = IsTransient;
            _ensureSuccess = false;

            return (TSelf)this;
        }

        /// <summary>
        /// Determines whether the <paramref name="response"/> or <paramref name="exception"/> result is transient in nature, and as such is a candidate for a retry.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="exception">The <see cref="Exception"/>.</param>
        /// <returns><c>true</c> indicates transient; otherwaise, <c>false</c>.</returns>
        public static bool IsTransient(HttpResponseMessage? response = null, Exception? exception = null)
        {
            if (exception != null)
            {
                if (exception is HttpRequestException)
                    return true;

                if (exception is TaskCanceledException)
                    return true;
            }

            if (response == null)
                return false;

            if ((int)response.StatusCode >= 500)
                return true;

            if (response.StatusCode == HttpStatusCode.RequestTimeout)
                return true;

            return false;
        }
    }
}