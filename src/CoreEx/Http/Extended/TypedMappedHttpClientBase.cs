// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Json;
using CoreEx.Mapping;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http.Extended
{
    /// <summary>
    /// Represents a typed <see cref="HttpClient"/> base wrapper with request/response <see cref="Mapper"/> support.
    /// </summary>
    /// <typeparam name="TSelf">The self <see cref="Type"/> for support fluent-style method-chaining.</typeparam>
    /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
    /// <param name="mapper">The <see cref="IMapper"/>.</param>
    /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
    /// <param name="executionContext">The <see cref="ExecutionContext"/>.</param>
    /// <param name="settings">The <see cref="SettingsBase"/>.</param>
    /// <param name="logger">The <see cref="ILogger"/>.</param>
    public abstract class TypedMappedHttpClientBase<TSelf>(HttpClient client, IMapper mapper, IJsonSerializer jsonSerializer, ExecutionContext executionContext, SettingsBase settings, ILogger<TypedMappedHttpClientBase<TSelf>> logger)
        : TypedHttpClientBase<TSelf>(client, jsonSerializer, executionContext, settings, logger), ITypedMappedHttpClient where TSelf : TypedMappedHttpClientBase<TSelf>
    {
        /// <summary>
        /// Gets the <see cref="IMapper"/>.
        /// </summary>
        public IMapper Mapper { get; } = mapper.ThrowIfNull(nameof(mapper));

        /// <summary>
        /// Maps the <typeparamref name="TResponseHttp"/> value to the <typeparamref name="TResponse"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="httpResult">The <see cref="HttpResult{T}"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected HttpResult<TResponse> MapResponse<TResponse, TResponseHttp>(HttpResult<TResponseHttp> httpResult) => (this as ITypedMappedHttpClient).MapResponse<TResponse, TResponseHttp>(httpResult);

        /// <summary>
        /// Maps the <typeparamref name="TRequest"/> <paramref name="value"/> to the <typeparamref name="TRequestHttp"/> <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TRequestHttp">The request HTTP <see cref="Type"/>.</typeparam>
        /// <param name="value">The request value.</param>
        /// <param name="operationType">The singluar <see href="https://en.wikipedia.org/wiki/Create,_read,_update_and_delete">CRUD</see> <see cref="OperationTypes"/> value being performed.</param>
        /// <returns>The mapped <typeparamref name="TRequestHttp"/> value.</returns>
        protected TRequestHttp MapRequest<TRequest, TRequestHttp>(TRequest value, OperationTypes operationType) => (this as ITypedMappedHttpClient).MapRequest<TRequest, TRequestHttp>(value, operationType);

        /// <summary>
        /// Send a <see cref="HttpMethod.Get"/> request to the specified Uri as an asynchronous operation and deserialize the JSON <see cref="HttpResponseMessage.Content"/> to the specified <typeparamref name="TResponse"/> <see cref="Type"/> (mapped from <typeparamref name="TResponseHttp"/>).
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> GetMappedAsync<TResponse, TResponseHttp>(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => MapResponse<TResponse, TResponseHttp>(await GetAsync<TResponseHttp>(requestUri, requestOptions, args, cancellationToken).ConfigureAwait(false));

        #region PostMappedAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> (mapped to <typeparamref name="TRequestHttp"/>).
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TRequestHttp">The request HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected Task<HttpResult> PostMappedAsync<TRequest, TRequestHttp>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => PostAsync(requestUri, MapRequest<TRequest, TRequestHttp>(value, OperationTypes.Create), requestOptions, args, cancellationToken);

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation and deserialize the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/> (mapped from <typeparamref name="TResponseHttp"/>).
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PostMappedAsync<TResponse, TResponseHttp>(string requestUri, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => MapResponse<TResponse, TResponseHttp>(await PostAsync<TResponseHttp>(requestUri, requestOptions, args, cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/> (mapped from <typeparamref name="TResponseHttp"/>).
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PostMappedAsync<TResponse, TResponseHttp>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => MapResponse<TResponse, TResponseHttp>(await PostAsync<TResponseHttp>(requestUri, content, requestOptions, args, cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Send a <see cref="HttpMethod.Post"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> (mapped to <typeparamref name="TRequestHttp"/>) and deserialize the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/> (mapped from <typeparamref name="TResponseHttp"/>).
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TRequestHttp">The request HTTP <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PostMappedAsync<TRequest, TRequestHttp, TResponse, TResponseHttp>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => MapResponse<TResponse, TResponseHttp>(await PostAsync<TRequestHttp, TResponseHttp>(requestUri, MapRequest<TRequest, TRequestHttp>(value, OperationTypes.Create), requestOptions, args, cancellationToken).ConfigureAwait(false));

        #endregion

        #region PutMappedAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/> (mapped from <typeparamref name="TResponseHttp"/>).
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PutMappedAsync<TResponse, TResponseHttp>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => MapResponse<TResponse, TResponseHttp>(await PutAsync<TResponseHttp>(requestUri, content, requestOptions, args, cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> (mapped to <typeparamref name="TRequestHttp"/>).
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TRequestHttp">The request HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpResult"/>.</returns>
        protected Task<HttpResult> PutMappedAsync<TRequest, TRequestHttp>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => PutAsync(requestUri, MapRequest<TRequest, TRequestHttp>(value, OperationTypes.Create), requestOptions, args, cancellationToken);

        /// <summary>
        /// Send a <see cref="HttpMethod.Put"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="value"/> (mapped to <typeparamref name="TRequestHttp"/>) and deserialize the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/> (mapped from <typeparamref name="TResponseHttp"/>).
        /// </summary>
        /// <typeparam name="TRequest">The request <see cref="Type"/>.</typeparam>
        /// <typeparam name="TRequestHttp">The request HTTP <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="value">The request value.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PutMappedAsync<TRequest, TRequestHttp, TResponse, TResponseHttp>(string requestUri, TRequest value, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => MapResponse<TResponse, TResponseHttp>(await PutAsync<TRequestHttp, TResponseHttp>(requestUri, MapRequest<TRequest, TRequestHttp>(value, OperationTypes.Create), requestOptions, args, cancellationToken).ConfigureAwait(false));

        #endregion

        #region PatchMappedAsync

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="content"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/> (mapped from <typeparamref name="TResponseHttp"/>).
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="content">The <see cref="HttpContent"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PatchMappedAsync<TResponse, TResponseHttp>(string requestUri, HttpContent content, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => MapResponse<TResponse, TResponseHttp>(await PatchAsync<TResponseHttp>(requestUri, content, requestOptions, args, cancellationToken));

        /// <summary>
        /// Send a <see cref="HttpMethod.Patch"/> request to the specified Uri as an asynchronous operation with the specified <paramref name="json"/> and deserializes the response JSON <see cref="HttpResponseMessage.Content"/> to <typeparamref name="TResponse"/> (mapped from <typeparamref name="TResponseHttp"/>).
        /// </summary>
        /// <typeparam name="TResponse">The response <see cref="Type"/>.</typeparam>
        /// <typeparam name="TResponseHttp">The response HTTP <see cref="Type"/>.</typeparam>
        /// <param name="requestUri">The Uri the request is sent to.</param>
        /// <param name="patchOption">The <see cref="HttpPatchOption"/>.</param>
        /// <param name="json">The JSON formatted as per the selected <paramref name="patchOption"/>.</param>
        /// <param name="requestOptions">The optional <see cref="HttpRequestOptions"/>.</param>
        /// <param name="args">Zero or more <see cref="IHttpArg"/> objects for <paramref name="requestUri"/> templating, query string additions, and content body specification.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The mapped <see cref="HttpResult{T}"/>.</returns>
        protected async Task<HttpResult<TResponse>> PatchMappedAsync<TResponse, TResponseHttp>(string requestUri, HttpPatchOption patchOption, string json, HttpRequestOptions? requestOptions = null, IEnumerable<IHttpArg>? args = null, CancellationToken cancellationToken = default)
            => MapResponse<TResponse, TResponseHttp>(await PatchAsync<TResponseHttp>(requestUri, patchOption, json, requestOptions, args, cancellationToken));

        #endregion
    }
}