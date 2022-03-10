// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents a typed <see cref="HttpClient"/> base wrapper that supports <see cref="HttpMethod.Head"/>, <see cref="HttpMethod.Get"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>, <see cref="HttpMethod.Patch"/> and <see cref="HttpMethod.Delete"/>.
    /// </summary>
    /// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
    public abstract class TypedHttpClientCore<TSelf> : TypedHttpClientBase<TypedHttpClientCore<TSelf>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientCore{TBase}"/>.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public TypedHttpClientCore(HttpClient client, IJsonSerializer jsonSerializer, ExecutionContext executionContext, SettingsBase settings, ILogger<TypedHttpClientCore<TSelf>> logger)
            : base(client, jsonSerializer, executionContext, settings, logger) { }

        #region HeadAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Head"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public Task<HttpResult> HeadAsync(string requestUri, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => HeadAsync(requestUri, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Head"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public async Task<HttpResult> HeadAsync(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(CreateRequest(HttpMethod.Head, requestUri, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        #endregion

        #region GetAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public Task<HttpResult> GetAsync(string requestUri, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => GetAsync(requestUri, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public async Task<HttpResult> GetAsync(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(CreateRequest(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation and deserialize the JSON <see cref="HttpResponseMessage.Content"/> to the specified .NET object <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult<TResponse>> GetAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => GetAsync<TResponse>(requestUri, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation and deserialize the JSON <see cref="HttpResponseMessage.Content"/> to the specified .NET object <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult<TResponse>> GetAsync<TResponse>(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(CreateRequest(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);
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
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult<TCollResult>> GetCollectionResultAsync<TCollResult, TColl, TItem>(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
            where TCollResult : ICollectionResult<TColl, TItem>, new()
            where TColl : ICollection<TItem>
        {
            var response = await SendAsync(CreateRequest(HttpMethod.Get, requestUri, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TCollResult, TColl, TItem>(response, JsonSerializer).ConfigureAwait(false);
        }

        #endregion

        #region PostAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public Task<HttpResult> PostAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PostAsync(requestUri, content, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public async Task<HttpResult> PostAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(CreateContentRequest(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public Task<HttpResult> PostAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PostAsync<TRequest>(requestUri, value, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public async Task<HttpResult> PostAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = (value is HttpContent content)
                ? await SendAsync(CreateContentRequest(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)
                : await SendAsync(CreateJsonRequest(HttpMethod.Post, requestUri, value, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);

            return await HttpResult.CreateAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PostAsync<TResponse>(requestUri, content, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult<TResponse>> PostAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(CreateContentRequest(HttpMethod.Post, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);
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
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PostAsync<TRequest, TResponse>(requestUri, value, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult<TResponse>> PostAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(CreateJsonRequest(HttpMethod.Post, requestUri, value, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);
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
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public Task<HttpResult> PutAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PutAsync(requestUri, content, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public async Task<HttpResult> PutAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(CreateContentRequest(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult> PutAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PutAsync<TRequest>(requestUri, value, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult> PutAsync<TRequest>(string requestUri, TRequest value, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = (value is HttpContent content)
                ? await SendAsync(CreateContentRequest(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)
                : await SendAsync(CreateJsonRequest(HttpMethod.Put, requestUri, value, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);

            return await HttpResult.CreateAsync(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult<TResponse>> PutAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PutAsync<TResponse>(requestUri, content, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult<TResponse>> PutAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(CreateContentRequest(HttpMethod.Put, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);
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
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PutAsync<TRequest, TResponse>(requestUri, value, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult<TResponse>> PutAsync<TRequest, TResponse>(string requestUri, TRequest value, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(CreateJsonRequest(HttpMethod.Put, requestUri, value, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);
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
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public Task<HttpResult> PatchAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PatchAsync(requestUri, content, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public async Task<HttpResult> PatchAsync(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(CreateContentRequest(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="json"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="patchOption">The <see cref="HttpPatchOption"/>.</param>
        /// <param name="json">The JSON formatted as per the selected <paramref name="patchOption"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult> PatchAsync(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions, params HttpArg[] args)
            => PatchAsync(requestUri, patchOption, json, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="json"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="patchOption">The <see cref="HttpPatchOption"/>.</param>
        /// <param name="json">The JSON formatted as per the selected <paramref name="patchOption"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult> PatchAsync(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            if (patchOption == HttpPatchOption.NotSpecified)
                throw new ArgumentException("A valid patch option must be specified.", nameof(patchOption));

            if (args != null && args.Any(x => x.ArgType == HttpArgType.FromBody))
                throw new ArgumentException("No arguments can be marked as IsFromBody for a PATCH.", nameof(args));

            var content = new StringContent(json, Encoding.UTF8, patchOption == HttpPatchOption.JsonPatch ? HttpConsts.JsonPatchMediaTypeName : HttpConsts.MergePatchMediaTypeName);
            return await HttpResult.CreateAsync(await SendAsync(CreateContentRequest(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, params HttpArg[] args)
            => PatchAsync<TResponse>(requestUri, content, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            var response = await SendAsync(CreateContentRequest(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);
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
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions, params HttpArg[] args)
            => PatchAsync<TResponse>(requestUri, patchOption, json, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="json"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="patchOption">The <see cref="HttpPatchOption"/>.</param>
        /// <param name="json">The JSON formatted as per the selected <paramref name="patchOption"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult{T}"/>.</returns>
        public async Task<HttpResult<TResponse>> PatchAsync<TResponse>(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
        {
            if (patchOption == HttpPatchOption.NotSpecified)
                throw new ArgumentException("A valid patch option must be specified.", nameof(patchOption));

            if (args != null && args.Any(x => x.ArgType == HttpArgType.FromBody))
                throw new ArgumentException("No arguments can be marked as IsFromBody for a PATCH.", nameof(args));

            var content = new StringContent(json, Encoding.UTF8, patchOption == HttpPatchOption.JsonPatch ? HttpConsts.JsonPatchMediaTypeName : HttpConsts.MergePatchMediaTypeName);
            var response = await SendAsync(CreateContentRequest(HttpMethod.Patch, requestUri, content, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false);
            return await HttpResult.CreateAsync<TResponse>(response, JsonSerializer).ConfigureAwait(false);
        }

        #endregion

        #region Delete

        /// <summary>
        /// Send a <see cref="HttpMethod.Delete"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public Task<HttpResult> DeleteAsync(string requestUri, HttpRequestOptions? requestOptions, params HttpArg[] args)
            => DeleteAsync(requestUri, requestOptions, args, default);

        /// <summary>
        /// Send a <see cref="HttpMethod.Delete"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="HttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        public async Task<HttpResult> DeleteAsync(string requestUri, HttpRequestOptions? requestOptions, IEnumerable<HttpArg>? args, CancellationToken cancellationToken = default)
            => await HttpResult.CreateAsync(await SendAsync(CreateRequest(HttpMethod.Delete, requestUri, requestOptions, args?.ToArray()!), cancellationToken).ConfigureAwait(false)).ConfigureAwait(false);

        #endregion
    }
}