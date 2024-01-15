// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Configuration;
using CoreEx.Entities;
using CoreEx.Http.Extended;
using CoreEx.Json;
using CoreEx.Mapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CoreEx.Http
{
    /// <summary>
    /// HTTP-related Extension methods.
    /// </summary>
    public static class HttpExtensions
    {
        /// <summary>
        /// Creates a <see cref="TypedHttpClient"/> for the <paramref name="httpClient"/>.
        /// </summary>
        /// <param name="httpClient">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>. Defaults to a new instance.</param>
        /// <param name="settings">The optional <see cref="SettingsBase"/>. Defaults to <see cref="DefaultSettings"/>.</param>
        /// <param name="logger">The optional <see cref="ILogger"/>. Defaults to <see cref="NullLogger{T}"/>.</param>
        /// <param name="onBeforeRequest">The optional <see cref="TypedHttpClientBase{TSelf}.OnBeforeRequest(HttpRequestMessage, CancellationToken)"/> function. Defaults to <c>null</c>.</param>
        /// <remarks><see cref="ExecutionContext.GetService{T}"/> is used to default each parameter to a configured service where present before final described defauls.</remarks>
        /// <returns>The <see cref="TypedHttpClient"/>.</returns>
        public static TypedHttpClient CreateTypedClient(this HttpClient httpClient, IJsonSerializer? jsonSerializer = null, ExecutionContext? executionContext = null, SettingsBase? settings = null, ILogger<TypedHttpClient>? logger = null, Func<HttpRequestMessage, CancellationToken, Task>? onBeforeRequest = null)
            => new(httpClient, jsonSerializer, executionContext, settings, logger, onBeforeRequest);

        /// <summary>
        /// Creates a <see cref="TypedMappedHttpClient"/> for the <paramref name="httpClient"/>.
        /// </summary>
        /// <param name="httpClient">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="mapper">The <see cref="IMapper"/>.</param>
        /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>. Defaults to a new instance.</param>
        /// <param name="settings">The optional <see cref="SettingsBase"/>. Defaults to <see cref="DefaultSettings"/>.</param>
        /// <param name="logger">The optional <see cref="ILogger"/>. Defaults to <see cref="NullLogger{T}"/>.</param>
        /// <param name="onBeforeRequest">The optional <see cref="TypedHttpClientBase{TSelf}.OnBeforeRequest(HttpRequestMessage, CancellationToken)"/> function. Defaults to <c>null</c>.</param>
        /// <remarks><see cref="ExecutionContext.GetService{T}"/> is used to default each parameter to a configured service where present before final described defauls.</remarks>
        /// <returns>The <see cref="TypedHttpClient"/>.</returns>
        public static TypedMappedHttpClient CreateTypedMappedClient(this HttpClient httpClient, IMapper? mapper = null, IJsonSerializer? jsonSerializer = null, ExecutionContext? executionContext = null, SettingsBase? settings = null, ILogger<TypedMappedHttpClient>? logger = null, Func<HttpRequestMessage, CancellationToken, Task>? onBeforeRequest = null)
            => new(httpClient, mapper, jsonSerializer, executionContext, settings, logger, onBeforeRequest);

        /// <summary>
        /// Applies the <see cref="HttpRequestOptions"/> to the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="requestOptions">The <see cref="HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequestMessage"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This will automatically invoke <see cref="ApplyETag(HttpRequestMessage, string)"/> where there is an <see cref="HttpRequestOptions.ETag"/> value.</remarks>
        public static HttpRequestMessage ApplyRequestOptions(this HttpRequestMessage httpRequest, HttpRequestOptions? requestOptions)
        {
            httpRequest.ThrowIfNull(nameof(httpRequest));

            if (requestOptions == null)
                return httpRequest;

            // Apply the ETag header.
            ApplyETag(httpRequest, requestOptions.ETag);

            // Apply updates to the query string.
            var qs = requestOptions.AddToQueryString(httpRequest.RequestUri?.Query);
            var ub = httpRequest.RequestUri == null ? new UriBuilder() : new UriBuilder(httpRequest.RequestUri);
            if (qs is not null)
                ub.Query = qs;

            httpRequest.RequestUri = ub.Uri;
            return httpRequest;
        }

        /// <summary>
        /// Applies the <i>ETag</i> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.Headers"/> as an <see cref="HttpRequestHeader.IfNoneMatch"/> (where <see cref="HttpRequestMessage.Method"/> is <see cref="HttpMethod.Get"/>
        /// or <see cref="HttpMethod.Head"/>); otherwise, an <see cref="HttpRequestHeader.IfMatch"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="etag">The <i>ETag</i> value.</param>
        /// <returns>The <see cref="HttpRequestMessage"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Automatically adds quoting to be ETag format compliant.</remarks>
        public static HttpRequestMessage ApplyETag(this HttpRequestMessage httpRequest, string? etag)
        {
            // Apply the ETag header.
            if (!string.IsNullOrEmpty(etag))
            {
                if (httpRequest.Method == HttpMethod.Get || httpRequest.Method == HttpMethod.Head)
                    httpRequest.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(ETagGenerator.FormatETag(etag)!));
                else
                    httpRequest.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(ETagGenerator.FormatETag(etag)!));
            }

            return httpRequest;
        }

        /// <summary>
        /// Trys to get the <see cref="PagingResult"/> from the <paramref name="response"/> headers.
        /// </summary>
        /// <param name="response">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="result">The <see cref="PagingResult"/> where found.</param>
        /// <returns><c>true</c> where the <see cref="PagingResult"/> is found; otherwise, <c>false</c>.</returns>
        public static bool TryGetPagingResult(this HttpResponseMessage response, [NotNullWhen(true)] out PagingResult? result)
        {
            var skip = ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingSkipHeaderName, out var vs) ? vs : null);
            var page = skip.HasValue ? null : ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingPageNumberHeaderName, out var vpn) ? vpn : null);
            var token = TryGetHeaderValue(response, HttpConsts.PagingTokenHeaderName, out var vtk) ? vtk : null;

            if (!string.IsNullOrEmpty(token))
                result = new PagingResult(PagingArgs.CreateTokenAndTake(token, ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingTakeHeaderName, out var vt) ? vt : null)));
            else if (skip.HasValue)
                result = new PagingResult(PagingArgs.CreateSkipAndTake(skip.Value, ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingTakeHeaderName, out var vt) ? vt : null)));
            else if (page.HasValue)
                result = new PagingResult(PagingArgs.CreatePageAndSize(page.Value, ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingPageSizeHeaderName, out var vps) ? vps : null)));
            else
            {
                result = null;
                return false;
            }

            result.TotalCount = ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingTotalCountHeaderName, out var vtc) ? vtc : null);
            return true;
        }

        /// <summary>
        /// Trys to get the first named <see cref="HttpResponseMessage.Headers"/> value from the <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value where found.</param>
        /// <returns><c>true</c> where the header value is found; otherwise, <c>false</c>.</returns>
        public static bool TryGetHeaderValue(this HttpResponseMessage response, string name, [NotNullWhen(true)] out string? value)
        {
            value = null;
            if (response == null || response.Headers == null || string.IsNullOrEmpty(name))
                return false;

            if (response.Headers.TryGetValues(name, out IEnumerable<string>? values))
            {
                value = values.First();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts the <see cref="HttpResponseMessage"/> to the equivalent <see cref="IExtendedException"/> based on the <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IExtendedException"/> where applicable; otherwise, <c>null</c>.</returns>
        public async static Task<IExtendedException?> ToExtendedExceptionAsync(this HttpResponseMessage response, bool useContentAsErrorMessage = true, CancellationToken cancellationToken = default)
        {
            if (response == null || response.IsSuccessStatusCode)
                return null;

#if NETSTANDARD2_1
            var content = response.Content == null ? null : await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#else
            var content = response.Content == null ? null : await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#endif
            if (string.IsNullOrEmpty(content))
                content = $"Response status code does not indicate success: {(int)response.StatusCode} ({(string.IsNullOrEmpty(response.ReasonPhrase) ? response.StatusCode : response.ReasonPhrase)}).";

            return HttpResultBase.CreateExtendedException(response, content, useContentAsErrorMessage);
        }

        /// <summary>
        /// Parses the value as a <see cref="long"/>.
        /// </summary>
        public static long? ParseLongValue(string? value)
        {
            if (value == null)
                return null;

            if (!long.TryParse(value, out long val))
                return null;

            return val;
        }

        /// <summary>
        /// Parses the value as a <see cref="bool"/>.
        /// </summary>
        public static bool ParseBoolValue(string? value)
        {
            if (value == null)
                return false;

            if (!bool.TryParse(value, out bool val))
                return false;

            return val;
        }
    }
}