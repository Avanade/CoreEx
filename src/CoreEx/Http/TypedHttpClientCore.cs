// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a typed <see cref="HttpClient"/> base wrapper that supports <see cref="HttpMethod.Head"/>, <see cref="HttpMethod.Get"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>, <see cref="HttpMethod.Patch"/> and <see cref="HttpMethod.Delete"/>.
    /// </summary>
    /// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
    /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
    /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
    /// <param name="executionContext">The optional <see cref="ExecutionContext"/>. Defaults to a new instance.</param>
    /// <remarks><see cref="ExecutionContext.GetService{T}"/> is used to default each parameter to a configured service where present before final described defaults.</remarks>
    public abstract class TypedHttpClientCore<TSelf>(HttpClient client, IJsonSerializer? jsonSerializer = null, ExecutionContext? executionContext = null) : TypedHttpClientBase<TSelf>(client, jsonSerializer, executionContext) where TSelf : TypedHttpClientCore<TSelf>
    {
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
        public new Task<HttpResult> HeadAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> HeadAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.HeadAsync(requestUri, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> GetAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> GetAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.GetAsync(requestUri, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult<TResponse>> GetAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult<TResponse>> GetAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.GetAsync<TResponse>(requestUri, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> PostAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> PostAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PostAsync(requestUri, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> PostAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> PostAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PostAsync(requestUri, content, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> PostAsync<TRequest>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> PostAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PostAsync<TRequest>(requestUri, value, requestOptions, args, cancellationToken);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation and deserialize the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
#if NET7_0_OR_GREATER
        public new Task<HttpResult<TResponse>> PostAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PostAsync<TResponse>(requestUri, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult<TResponse>> PostAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PostAsync<TResponse>(requestUri, content, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PostAsync<TRequest, TResponse>(requestUri, value, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> PutAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> PutAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PutAsync(requestUri, content, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> PutAsync<TRequest>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> PutAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PutAsync<TRequest>(requestUri, value, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult<TResponse>> PutAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult<TResponse>> PutAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PutAsync<TResponse>(requestUri, content, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PutAsync<TRequest, TResponse>(requestUri, value, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> PatchAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> PatchAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PatchAsync(requestUri, content, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> PatchAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpPatchOption patchOption, [StringSyntax(StringSyntaxAttribute.Json)] string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> PatchAsync(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PatchAsync(requestUri, patchOption, json, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult<TResponse>> PatchAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PatchAsync<TResponse>(requestUri, content, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult<TResponse>> PatchAsync<TResponse>([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpPatchOption patchOption, [StringSyntax(StringSyntaxAttribute.Json)] string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.PatchAsync<TResponse>(requestUri, patchOption, json, requestOptions, args, cancellationToken);

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
        public new Task<HttpResult> DeleteAsync([StringSyntax(StringSyntaxAttribute.Uri)] string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#else
        public new Task<HttpResult> DeleteAsync(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
#endif
            => base.DeleteAsync(requestUri, requestOptions, args, cancellationToken);

        #endregion
    }
}