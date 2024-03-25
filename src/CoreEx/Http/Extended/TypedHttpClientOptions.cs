// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http.Extended
{
    /// <summary>
    /// Represents the <see cref="TypedHttpClientBase{TSelf}"/> <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/> options.
    /// </summary>
    public sealed class TypedHttpClientOptions
    {
        private readonly TypedHttpClientOptions? _defaultOptions;
        private readonly ITypedHttpClientOptions? _owner;
        private List<HttpStatusCode>? _ensureStatusCodes;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientOptions"/> class.
        /// </summary>
        /// <param name="defaultOptions">Optional default <see cref="TypedHttpClientOptions"/> to copy from; also copied as a result of a <see cref="Reset"/>.</param>
        public TypedHttpClientOptions(TypedHttpClientOptions? defaultOptions = null)
        {
            _defaultOptions = defaultOptions;
            if (_defaultOptions is not null)
                Reset();
        }

        /// <summary>
        /// Initializes a new instance of the <i>Default</i> <see cref="TypedHttpClientOptions"/> class.
        /// </summary>
        /// <param name="owner">The <see cref="ITypedHttpClientOptions"/>.</param>
        internal TypedHttpClientOptions(ITypedHttpClientOptions owner)
        {
            _owner = owner;
            CheckDefaultNotBeingUpdatedInSendMode();
        }

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
        /// Indicates whether to check the <see cref="HttpResponseMessage"/> and where considered a transient error then a <see cref="TransientException"/> will be thrown.
        /// </summary>
        /// <param name="predicate">An optional predicate to determine whether the error is considered transient. Defaults to <see cref="TypedHttpClientBase.IsTransient(HttpResponseMessage?, Exception?)"/> where not specified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
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
        /// <remarks>This is <see cref="Reset"/> after each invocation; see <see cref="TypedHttpClientBase.SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TypedHttpClientOptions ThrowKnownException(bool useContentAsErrorMessage = false)
        {
            CheckDefaultNotBeingUpdatedInSendMode();
            ShouldThrowKnownException = true;
            ShouldThrowKnownUseContentAsMessage = useContentAsErrorMessage;
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
                ShouldThrowTransientException = false;
                IsTransientPredicate = TypedHttpClientBase.IsTransient;
                ShouldThrowKnownException = false;
                ShouldThrowKnownUseContentAsMessage = false;
                ShouldEnsureSuccess = false;
                _ensureStatusCodes = null;
                ShouldNullOnNotFound = false;
                BeforeRequest = null;
            }
            else
            {
                ShouldThrowTransientException = _defaultOptions.ShouldThrowTransientException;
                IsTransientPredicate = _defaultOptions.IsTransientPredicate;
                ShouldThrowKnownException = _defaultOptions.ShouldThrowKnownException;
                ShouldThrowKnownUseContentAsMessage = _defaultOptions.ShouldThrowKnownUseContentAsMessage;
                ShouldEnsureSuccess = _defaultOptions.ShouldEnsureSuccess;
                _ensureStatusCodes = _defaultOptions.ExpectedStatusCodes == null ? null : new(_defaultOptions.ExpectedStatusCodes);
                ShouldNullOnNotFound = _defaultOptions.ShouldNullOnNotFound;
                BeforeRequest = _defaultOptions.BeforeRequest;
            }
        }
    }
}