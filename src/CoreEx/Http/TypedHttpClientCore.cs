// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Http;
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
        /// <param name="settings">The <see cref="SettingsBase"/>.</param>
        /// <param name="logger">The <see cref="ILogger"/>.</param>
        public TypedHttpClientCore(HttpClient client, IJsonSerializer jsonSerializer, SettingsBase settings, ILogger<TypedHttpClientCore<TSelf>> logger)
            : base(client, jsonSerializer, settings, logger) { }

        /// <summary>
        /// Send a <see cref="HttpMethod.Head"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> HeadAsync(string requestUri, CancellationToken cancellationToken = default)
            => await SendAsync(CreateRequest(HttpMethod.Head, requestUri), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> GetAsync(string requestUri, CancellationToken cancellationToken = default)
            => await SendAsync(CreateRequest(HttpMethod.Get, requestUri), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation and deserialize the JSON <see cref="HttpResponseMessage.Content"/> to the specified .NET object <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The response value.</returns>
        public async Task<TResponse> GetAsync<TResponse>(string requestUri, CancellationToken cancellationToken = default)
        {
            var response = await GetAsync(requestUri, cancellationToken).ConfigureAwait(false);
            return await ReadAsJsonAsync<TResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> PostAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => await SendAsync(CreateRequest(HttpMethod.Post, requestUri, content), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> PostAsync<TRequest>(string requestUri, TRequest value, CancellationToken cancellationToken = default)
            => (value is HttpContent content)
                ? await PostAsync(requestUri, content, cancellationToken).ConfigureAwait(false)
                : await SendAsync(CreateJsonRequest(HttpMethod.Post, requestUri, value), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The response value.</returns>
        public async Task<TResponse> PostAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync(requestUri, content, cancellationToken);
            return await ReadAsJsonAsync<TResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The response value.</returns>
        public async Task<TResponse> PostAsync<TRequest, TResponse>(string requestUri, TRequest value, CancellationToken cancellationToken = default)
        {
            var response = await PostAsync(requestUri, value, cancellationToken).ConfigureAwait(false);
            return await ReadAsJsonAsync<TResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> PutAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => await SendAsync(CreateRequest(HttpMethod.Put, requestUri, content), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> PutAsync<TRequest>(string requestUri, TRequest value, CancellationToken cancellationToken = default)
            => (value is HttpContent content)
                ? await PutAsync(requestUri, content, cancellationToken).ConfigureAwait(false)
                : await SendAsync(CreateJsonRequest(HttpMethod.Put, requestUri, value), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The response value.</returns>
        public async Task<TResponse> PutAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            var response = await PutAsync(requestUri, content, cancellationToken);
            return await ReadAsJsonAsync<TResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The response value.</returns>
        public async Task<TResponse> PutAsync<TRequest, TResponse>(string requestUri, TRequest value, CancellationToken cancellationToken = default)
        {
            var response = await PutAsync(requestUri, value, cancellationToken).ConfigureAwait(false);
            return await ReadAsJsonAsync<TResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> PatchAsync(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
            => await SendAsync(CreateRequest(HttpMethod.Patch, requestUri, content), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> PatchAsync<TRequest>(string requestUri, TRequest value, CancellationToken cancellationToken = default)
            => (value is HttpContent content)
                ? await PatchAsync(requestUri, content, cancellationToken).ConfigureAwait(false)
                : await SendAsync(CreateJsonRequest(HttpMethod.Patch, requestUri, value), cancellationToken).ConfigureAwait(false);

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The response value.</returns>
        public async Task<TResponse> PatchAsync<TResponse>(string requestUri, HttpContent content, CancellationToken cancellationToken = default)
        {
            var response = await PatchAsync(requestUri, content, cancellationToken);
            return await ReadAsJsonAsync<TResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> and deserialize the JSON <see cref="HttpResponseMessage.Content"/> to the specified .NET object <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The response value.</returns>
        public async Task<TResponse> PatchAsync<TRequest, TResponse>(string requestUri, TRequest value, CancellationToken cancellationToken = default)
        {
            var response = await PatchAsync(requestUri, value, cancellationToken).ConfigureAwait(false);
            return await ReadAsJsonAsync<TResponse>(response).ConfigureAwait(false);
        }

        /// <summary>
        /// Send a <see cref="HttpMethod.Delete"/> request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResponseMessage"/>.</returns>
        public async Task<HttpResponseMessage> DeleteAsync(string requestUri, CancellationToken cancellationToken = default)
            => await SendAsync(CreateRequest(HttpMethod.Delete, requestUri), cancellationToken).ConfigureAwait(false);
    }
}