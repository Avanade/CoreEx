// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using Polly;
using Polly.Extensions.Http;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http.Extended
{
    /// <summary>
    /// Represents the <see cref="TypedHttpClientBase{TSelf}"/> <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/> options.
    /// </summary>
    public sealed class TypedHttpClientOptions
    {
        private readonly SettingsBase _settings;
        private readonly TypedHttpClientOptions? _defaultOptions;
        private readonly ITypedHttpClientOptions? _owner;
        private List<HttpStatusCode>? _ensureStatusCodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientOptions"/> class.
        /// </summary>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="defaultOptions">Optional default <see cref="TypedHttpClientOptions"/> to copy from; also copied as a result of a <see cref="Reset"/>.</param>
        public TypedHttpClientOptions(SettingsBase settings, TypedHttpClientOptions? defaultOptions = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _defaultOptions = defaultOptions;
            if (_defaultOptions is not null)
                Reset();
        }

        /// <summary>
        /// Initializes a new instance of the <i>Default</i> <see cref="TypedHttpClientOptions"/> class.
        /// </summary>
        /// <param name="owner">The <see cref="ITypedHttpClientOptions"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        internal TypedHttpClientOptions(ITypedHttpClientOptions owner, SettingsBase settings)
        {
            _owner = owner;
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            CheckDefaultNotBeingUpdatedInSendMode();
        }

        /// <summary>
        /// Gets the retry count; see <see cref="WithRetry(int?, double?)"/>.
        /// </summary>
        public int? RetryCount { get; private set; }

        /// <summary>
        /// Gets the retry seconds; see <see cref="WithRetry(int?, double?)"/>.
        /// </summary>
        public double? RetrySeconds { get; private set; }

        /// <summary>
        /// Indicates whether to ensure success; see <see cref="EnsureSuccess"/>.
        /// </summary>
        public bool ShouldEnsureSuccess { get; private set; }

        /// <summary>
        /// Gets the list of expected status codes; see <see cref="Ensure"/>.
        /// </summary>
        public ReadOnlyCollection<HttpStatusCode>? ExpectedStatusCodes => _ensureStatusCodes == null ? null : new(_ensureStatusCodes);

        /// <summary>
        /// Indicates whether a <see cref="TransientException"/> should be thrown; see <see cref="ThrowTransientException"/>.
        /// </summary>
        public bool ShouldThrowTransientException { get; private set; }

        /// <summary>
        /// Gets the predicate that determines whether is transient; see <see cref="ThrowTransientException"/>.
        /// </summary>
        public Func<HttpResponseMessage?, Exception?, (bool result, string error)> IsTransientPredicate { get; private set; } = TypedHttpClientBase.IsTransient;

        /// <summary>
        /// Indicates whether a known exception is thrown; see <see cref="ThrowKnownException(bool)"/>.
        /// </summary>
        public bool ShouldThrowKnownException { get; private set; }

        /// <summary>
        /// Indicates whether the content should be thrown in the known exception; see <see cref="ThrowKnownException(bool)"/>.
        /// </summary>
        public bool ShouldThrowKnownUseContentAsMessage { get; private set; }

        /// <summary>
        /// Gets the custom retry policy; see <see cref="WithCustomRetryPolicy(PolicyBuilder{HttpResponseMessage})"/>.
        /// </summary>
        public PolicyBuilder<HttpResponseMessage>? CustomRetryPolicy { get; private set; }

        /// <summary>
        /// Gets the timeout; see <see cref="WithTimeout(TimeSpan)"/>.
        /// </summary>
        public TimeSpan? Timeout { get; private set; }

        /// <summary>
        /// Gets the maximum retry delay; see <see cref="WithMaxRetryDelay(TimeSpan)"/>.
        /// </summary>
        public TimeSpan? MaxRetryDelay { get; private set; }

        /// <summary>
        /// Indicates that a <c>null/default</c> is to be returned where the <b>response</b> has a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/> (on <see cref="HttpMethod.Get"/> only).
        /// </summary>
        public bool ShouldNullOnNotFound { get; private set; }

        /// <summary>
        /// Gets the function to update the <see cref="HttpRequestMessage"/> before the request is sent; see <see cref="OnBeforeRequest(Func{HttpRequestMessage, CancellationToken, Task}?)"/>.
        /// </summary>
        public Func<HttpRequestMessage, CancellationToken, Task>? BeforeRequest { get; private set; }

        /// <summary>
        /// Checks whether the default is being updated when in send mode which is not allowed.
        /// </summary>
        private void CheckDefaultNotBeingUpdatedInSendMode()
        {
            if (_owner is not null && _owner.HasSendOptions)
                throw new InvalidOperationException($"The {nameof(ITypedHttpClientOptions.DefaultOptions)} can not be updated where individual {nameof(ITypedHttpClientOptions.SendOptions)} have been configured; must first perform a TypedHttpClientBase<TSelf>.SendAsync or TypedHttpClientBase<TSelf>.Reset to update.");
        }

        /// <summary>
        /// Sets the underlying retry policy using the specified custom <see cref="PolicyBuilder{TResult}"/>.
        /// </summary>
        /// <param name="retryPolicy">The custom retry policy.</param>
        /// <remarks>Defaults to <see cref="HttpPolicyExtensions.HandleTransientHttpError"/> with additional handling of <see cref="SocketException"/> and <see cref="TimeoutException"/>.
        /// <para>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</para></remarks>
        public TypedHttpClientOptions WithCustomRetryPolicy(PolicyBuilder<HttpResponseMessage> retryPolicy)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            CustomRetryPolicy = retryPolicy ?? throw new ArgumentNullException(nameof(retryPolicy));
            return this;
        }

        /// <summary>
        /// Indicates whether to check the <see cref="HttpResponseMessage"/> and where considered a transient error then a <see cref="TransientException"/> will be thrown.
        /// </summary>
        /// <param name="predicate">An optional predicate to determine whether the error is considered transient. Defaults to <see cref="TypedHttpClientBase.IsTransient(HttpResponseMessage?, Exception?)"/> where not specified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This occurs outside of any <see cref="WithRetry(int?, double?)"/>.<para>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</para></remarks>
        public TypedHttpClientOptions ThrowTransientException(Func<HttpResponseMessage?, Exception?, (bool result, string error)>? predicate = null)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            ShouldThrowTransientException = true;
            IsTransientPredicate = predicate ?? TypedHttpClientBase.IsTransient;
            return this;
        }

        /// <summary>
        /// Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be thrown.
        /// </summary>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This occurs outside of any <see cref="WithRetry(int?, double?)"/>.<para>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</para></remarks>
        public TypedHttpClientOptions ThrowKnownException(bool useContentAsErrorMessage = false)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            ShouldThrowKnownException = true;
            ShouldThrowKnownUseContentAsMessage = useContentAsErrorMessage;
            return this;
        }

        /// <summary>
        /// Indicates whether to perform a retry where an underlying transient error occurs.
        /// </summary>
        /// <param name="count">The number of times to retry. Defaults to <see cref="SettingsBase.HttpRetryCount"/>.</param>
        /// <param name="seconds">The base number of seconds to delay between retries. Defaults to <see cref="SettingsBase.HttpRetrySeconds"/>. Delay will be exponential with each retry.</param>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>The <paramref name="count"/> is the number of additional retries that should be performed in addition to the initial request.</para></remarks>
        public TypedHttpClientOptions WithRetry(int? count = null, double? seconds = null)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            var retryCount = (_owner is null ? null : _settings.GetValue<int?>($"{_owner.GetType().Name}__{nameof(SettingsBase.HttpRetryCount)}")) ?? _settings.HttpRetryCount;
            var retrySeconds = (_owner is null ? null : _settings.GetValue<double?>($"{_owner.GetType().Name}__{nameof(SettingsBase.HttpRetrySeconds)}")) ?? _settings.HttpRetrySeconds;

            RetryCount = count ?? retryCount;
            if (RetryCount < 0)
                RetryCount = retryCount;

            RetrySeconds = seconds ?? retrySeconds;
            if (RetrySeconds < 0)
                RetrySeconds = retrySeconds;

            return this;
        }

        /// <summary>
        /// Indicates whether to automatically perform an <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TypedHttpClientOptions EnsureSuccess()
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            ShouldEnsureSuccess = true;
            return this;
        }

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.OK"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TypedHttpClientOptions EnsureOK() => Ensure(HttpStatusCode.OK);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.NoContent"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TypedHttpClientOptions EnsureNoContent() => Ensure(HttpStatusCode.NoContent);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.Accepted"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TypedHttpClientOptions EnsureAccepted() => Ensure(HttpStatusCode.Accepted);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.Created"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TypedHttpClientOptions EnsureCreated() => Ensure(HttpStatusCode.Created);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.NotFound"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TypedHttpClientOptions EnsureNotFound() => Ensure(HttpStatusCode.NotFound);

        /// <summary>
        /// Adds the <paramref name="statusCodes"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <param name="statusCodes">One or more <see cref="HttpStatusCode">status codes</see> to be verified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TypedHttpClientOptions Ensure(params HttpStatusCode[] statusCodes)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            if (statusCodes != null && statusCodes.Length > 0)
            {
                if (_ensureStatusCodes == null)
                    _ensureStatusCodes = new List<HttpStatusCode>(statusCodes);
                else
                    _ensureStatusCodes.AddRange(statusCodes);
            }

            return this;
        }

        /// <summary>
        /// Sets timeout for given request
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TypedHttpClientOptions WithTimeout(TimeSpan timeout)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            Timeout = timeout;
            return this;
        }

        /// <summary>
        /// Sets max retry delay that polly retries will be capped with (this affects mostly 429 and 503 responses that can return Retry-After header).
        /// Default is 30s but it can be overridden for async calls (e.g. when using service bus trigger).
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TypedHttpClientOptions WithMaxRetryDelay(TimeSpan maxRetryDelay)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            MaxRetryDelay = maxRetryDelay;
            return this;
        }

        /// <summary>
        /// Indicates that a <c>null/default</c> is to be returned where the <b>response</b> has a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/> (on <see cref="HttpMethod.Get"/> only).
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TypedHttpClientOptions NullOnNotFound()
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            ShouldNullOnNotFound = true;
            return this;
        }

        /// <summary>
        /// Sets the function to update the <see cref="HttpRequestMessage"/> before the request is sent.
        /// </summary>
        /// <param name="beforeRequest">The function to update the <see cref="HttpRequestMessage"/>.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TypedHttpClientOptions OnBeforeRequest(Func<HttpRequestMessage, CancellationToken, Task>? beforeRequest)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            BeforeRequest = beforeRequest;
            return this;
        }

        /// <summary>
        /// Resets the <see cref="TypedHttpClientOptions"/> to its default state.
        /// </summary>
        public void Reset()
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            if (_defaultOptions is null)
            {
                CustomRetryPolicy = null;
                RetryCount = null;
                RetrySeconds = null;
                ShouldThrowTransientException = false;
                IsTransientPredicate = TypedHttpClientBase.IsTransient;
                ShouldThrowKnownException = false;
                ShouldThrowKnownUseContentAsMessage = false;
                ShouldEnsureSuccess = false;
                _ensureStatusCodes = null;
                Timeout = null;
                MaxRetryDelay = null;
                ShouldNullOnNotFound = false;
                BeforeRequest = null;
            }
            else
            {
                CustomRetryPolicy = _defaultOptions.CustomRetryPolicy;
                RetryCount = _defaultOptions.RetryCount;
                RetrySeconds = _defaultOptions.RetrySeconds;
                ShouldThrowTransientException = _defaultOptions.ShouldThrowTransientException;
                IsTransientPredicate = _defaultOptions.IsTransientPredicate;
                ShouldThrowKnownException = _defaultOptions.ShouldThrowKnownException;
                ShouldThrowKnownUseContentAsMessage = _defaultOptions.ShouldThrowKnownUseContentAsMessage;
                ShouldEnsureSuccess = _defaultOptions.ShouldEnsureSuccess;
                _ensureStatusCodes = _defaultOptions.ExpectedStatusCodes == null ? null : new(_defaultOptions.ExpectedStatusCodes);
                Timeout = _defaultOptions.Timeout;
                MaxRetryDelay = _defaultOptions.MaxRetryDelay;
                ShouldNullOnNotFound = _defaultOptions.ShouldNullOnNotFound;
                BeforeRequest = _defaultOptions.BeforeRequest;
            }
        }
    }
}