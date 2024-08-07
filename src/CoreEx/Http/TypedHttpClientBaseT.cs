﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Http.Extended;
using CoreEx.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a typed <see cref="HttpClient"/> base wrapper.
    /// </summary>
    /// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
    public abstract class TypedHttpClientBase<TSelf> : TypedHttpClientBase, ITypedHttpClientOptions where TSelf : TypedHttpClientBase<TSelf>
    {
        private TypedHttpClientOptions? _defaultOptions;
        private TypedHttpClientOptions? _sendOptions;

        /// <summary>
        /// Gets or sets an action that is invoked during instance instantiation of the <see cref="TypedHttpClientBase{TSelf}"/> to enable additional runtime configuration of the <see cref="DefaultOptions"/> to be performed on the instance itself.
        /// </summary>
        /// <remarks>This is required as there is currently no means to configure prior to usage when using Dependency Injection (DI) leveraging the out-of-the-box <see href="https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.httpclientfactoryservicecollectionextensions.addhttpclient">AddHttpClient</see>.</remarks>
        public static Action<TypedHttpClientOptions>? OnDefaultOptionsConfiguration { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientBase{TSelf}"/>.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
        /// <param name="executionContext">The optional <see cref="CoreEx.ExecutionContext"/>. Defaults to a new instance.</param>
        /// <remarks><see cref="ExecutionContext.GetService{T}"/> is used to default each parameter to a configured service where present before final described defaults.</remarks>
        public TypedHttpClientBase(HttpClient client, IJsonSerializer? jsonSerializer = null, ExecutionContext? executionContext = null) : base(client, jsonSerializer)
        {
            ExecutionContext = executionContext ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current : new ExecutionContext());
            OnDefaultOptionsConfiguration?.Invoke(DefaultOptions);
        }

        /// <summary>
        /// Gets the <see cref="CoreEx.ExecutionContext"/>.
        /// </summary>
        protected ExecutionContext ExecutionContext { get; }

        /// <summary>
        /// Gets the default <see cref="TypedHttpClientOptions"/> used by all invocations (<see cref="Reset"/> after each <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>).
        /// </summary>
        /// <remarks>It is recommended that a <see cref="Reset"/> is performed after setting (unless set within the constructor) to ensure latest values are used.</remarks>
        public TypedHttpClientOptions DefaultOptions => _defaultOptions ??= new TypedHttpClientOptions(this);

        /// <summary>
        /// Gets the <see cref="TypedHttpClientOptions"/> used <i>per</i> <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/> invocation then is immediately <see cref="Reset"/>.
        /// </summary>
        /// <remarks>This is <see cref="Reset"/> automatically after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>. The <see cref="Reset"/> will updated to the pre-configured <see cref="DefaultOptions"/>.</remarks>
        public TypedHttpClientOptions SendOptions => _sendOptions ??= new TypedHttpClientOptions(_defaultOptions);

        /// <inheritdoc/>
        bool ITypedHttpClientOptions.HasSendOptions => _sendOptions is not null;

        /// <summary>
        /// Gets the list of correlation header names.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.CorrelationIdHeaderName"/> and '<c>x-ms-client-tracking-id</c>'.</remarks>
        protected virtual IEnumerable<string> CorrelationHeaderNames { get; } = [HttpConsts.CorrelationIdHeaderName, "x-ms-client-tracking-id"];

        /// <summary>
        /// Indicates whether to check the <see cref="HttpResponseMessage"/> and where considered a transient error then a <see cref="TransientException"/> will be thrown.
        /// </summary>
        /// <param name="predicate">An optional predicate to determine whether the error is considered transient. Defaults to <see cref="TypedHttpClientBase.IsTransient(HttpResponseMessage?, Exception?)"/> where not specified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TSelf ThrowTransientException(Func<HttpResponseMessage?, Exception?, (bool result, string error)>? predicate = null)
        {
            SendOptions.ThrowTransientException(predicate);
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to check the <see cref="HttpResponseMessage.StatusCode"/> and where it matches one of the <i>known</i> <see cref="IExtendedException.StatusCode"/> values then that <see cref="IExtendedException"/> will be thrown.
        /// </summary>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TSelf ThrowKnownException(bool useContentAsErrorMessage = false)
        {
            SendOptions.ThrowKnownException(useContentAsErrorMessage);
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates whether to automatically perform an <see cref="HttpResponseMessage.EnsureSuccessStatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TSelf EnsureSuccess()
        {
            SendOptions.EnsureSuccess();
            return (TSelf)this;
        }

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.OK"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureOK() => Ensure(HttpStatusCode.OK);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.NoContent"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureNoContent() => Ensure(HttpStatusCode.NoContent);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.Accepted"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureAccepted() => Ensure(HttpStatusCode.Accepted);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.Created"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureCreated() => Ensure(HttpStatusCode.Created);

        /// <summary>
        /// Adds the <see cref="HttpStatusCode.NotFound"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf EnsureNotFound() => Ensure(HttpStatusCode.NotFound);

        /// <summary>
        /// Adds the <paramref name="statusCodes"/> to the accepted list to be verified against the resulting <see cref="HttpResponseMessage.StatusCode"/>.
        /// </summary>
        /// <param name="statusCodes">One or more <see cref="HttpStatusCode">status codes</see> to be verified.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Will result in a <see cref="HttpRequestException"/> where condition is not met.</para></remarks>
        public TSelf Ensure(params HttpStatusCode[] statusCodes)
        {
            SendOptions.Ensure(statusCodes);
            return (TSelf)this;
        }

        /// <summary>
        /// Indicates that a <c>null/default</c> is to be returned where the <b>response</b> has a <see cref="HttpStatusCode"/> of <see cref="HttpStatusCode.NotFound"/> (on <see cref="HttpMethod.Get"/> only).
        /// </summary>
        /// <remarks>
        /// This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.
        /// <para>Results in the corresponding <see cref="HttpResult"/> <see cref="HttpResultBase.NullOnNotFoundResponse"/> being set to get the desired outcome.</para></remarks>
        public TSelf NullOnNotFound()
        {
            SendOptions.NullOnNotFound();
            return (TSelf)this;
        }

        /// <summary>
        /// Sets the function to update the <see cref="HttpRequestMessage"/> before the request is sent.
        /// </summary>
        /// <param name="beforeRequest">The function to update the <see cref="HttpRequestMessage"/>.</param>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        /// <remarks>This references the equivalent method within the <see cref="SendOptions"/>. This is <see cref="Reset"/> after each invocation; see <see cref="SendAsync(HttpRequestMessage, CancellationToken)"/>.</remarks>
        public TSelf OnBeforeRequest(Func<HttpRequestMessage, CancellationToken, Task>? beforeRequest)
        {
            SendOptions.OnBeforeRequest(beforeRequest);
            return (TSelf)this;
        }

        /// <summary>
        /// Resets the <see cref="TypedHttpClientBase{TSelf}"/> to its initial <see cref="DefaultOptions"/> state.
        /// </summary>
        /// <returns>This instance to support fluent-style method-chaining.</returns>
        public virtual TSelf Reset()
        {
            _sendOptions = null;
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
            try
            {
                return await TypedHttpClientInvoker.Current.InvokeAsync(this, async (_, cancellationToken) => await SendInternalAsync(request, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                Reset();
            }
        }

        /// <summary>
        /// Sends the <paramref name="request"/> returning the <see cref="HttpResponseMessage"/> internal.
        /// </summary>
        private async Task<HttpResponseMessage> SendInternalAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            HttpResponseMessage? response = null;
            var options = SendOptions;

            try
            {
                CorrelationHeaderNames.ForEach(n => request.Headers.TryAddWithoutValidation(n, ExecutionContext.CorrelationId));

                if (options.BeforeRequest != null)
                    await options.BeforeRequest(request, cancellationToken).ConfigureAwait(false);

                await OnBeforeRequest(request, cancellationToken).ConfigureAwait(false);

                response = await Client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is TimeoutException || ex is SocketException)
            {
                // Both TimeoutException and SocketException are transient and indicate a connection was terminated.
                if (options.ShouldThrowTransientException)
                    throw new TransientException(ex.Message, ex);

                throw;
            }
            catch (HttpRequestException hrex)
            {
                (bool isTransient, string error) = options.IsTransientPredicate(null, hrex);
                if (options.ShouldThrowTransientException && isTransient)
                    throw new TransientException(error, hrex);

                throw;
            }

            // Further check if transient and throw accordingly.
            (bool wasTransient, string errorMsg) = options.IsTransientPredicate(response, null);
            if (options.ShouldThrowTransientException && wasTransient)
                throw new TransientException(errorMsg);

            if (options.ShouldThrowKnownException)
            {
                var eex = await response.ToExtendedExceptionAsync(options.ShouldThrowKnownUseContentAsMessage, cancellationToken).ConfigureAwait(false);
                if (eex != null)
                    throw (Exception)eex;
            }

            if (options.ShouldEnsureSuccess)
                response.EnsureSuccessStatusCode();

            if (options.ExpectedStatusCodes != null && !options.ExpectedStatusCodes.Contains(response.StatusCode))
                throw new HttpRequestException($"Response status code {response.StatusCode}; expected one of the following: {string.Join(", ", options.ExpectedStatusCodes)}.");

            return response;
        }

        /// <summary>
        /// Provides an opportunity to update the <paramref name="request"/> before sending.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/></param>
        protected virtual Task OnBeforeRequest(HttpRequestMessage request, CancellationToken cancellationToken) => Task.CompletedTask;

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
#if NET7_0_OR_GREATER
        public async Task<HttpResult> HeadAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public async Task<HttpResult> HeadAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> GetAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> GetAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
        {
            var nullOnNotFoundResponse = _sendOptions is not null && _sendOptions.ShouldNullOnNotFound;
            var hr = await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            hr.NullOnNotFoundResponse = nullOnNotFoundResponse;
            return hr;
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation and deserialize the JSON <see cref="HttpResponseMessage.Content"/> to the specified .NET object <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
#if NET7_0_OR_GREATER
        protected async Task<HttpResult<TResponse>> GetAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult<TResponse>> GetAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
        {
            var nullOnNotFoundResponse = _sendOptions is not null && _sendOptions.ShouldNullOnNotFound;
            var response = await SendAsync(await CreateRequestAsync(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);
            var hr = await HttpResult.CreateAsync<TResponse>(response, JsonSerializer, cancellationToken).ConfigureAwait(false);
            hr.NullOnNotFoundResponse = nullOnNotFoundResponse;
            return hr;
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> PostAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> PostAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> PostAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> PostAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> PostAsync<TRequest>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> PostAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult<TResponse>> PostAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult<TResponse>> PostAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args =  null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args =  null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args =  null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args =  null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> PutAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> PutAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> PutAsync<TRequest>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> PutAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult<TResponse>> PutAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult<TResponse>> PutAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> PatchAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> PatchAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> PatchAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpPatchOption patchOption, [StringSyntax(StringSyntaxAttribute.Json)] string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> PatchAsync(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult<TResponse>> PatchAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult<TResponse>> PatchAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpPatchOption patchOption, [StringSyntax(StringSyntaxAttribute.Json)] string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
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
#if NET7_0_OR_GREATER
        protected async Task<HttpResult> DeleteAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        protected async Task<HttpResult> DeleteAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => await HttpResult.CreateAsync(await SendAsync(await CreateRequestAsync(HttpMethod.Delete, requestUri, requestOptions, args?.ToArray()!, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

        #endregion

        #region Healthcheck

        /// <summary>
        /// Performs a health check by sending a <see cref="HttpMethod.Head"/> request to base Uri as an asynchronous operation.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public virtual Task<HttpResult> HealthCheckAsync(CancellationToken cancellationToken = default)
            => HeadAsync(string.Empty, null, null, cancellationToken);

        #endregion
    }
}