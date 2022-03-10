// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Extension methods for <see cref="HttpRequest"/>.
    /// </summary>
    public static class HttpExtensions
    {
        private const string _errorText = "Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted:";

        /// <summary>
        /// Applies the <see cref="HttpRequestOptions"/> to the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="requestOptions">The <see cref="HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequestMessage"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This will automatically invoke <see cref="ApplyETag(HttpRequestMessage, string)"/> where there is an <see cref="HttpRequestOptions.ETag"/> value.</remarks>
        public static HttpRequestMessage ApplyRequestOptions(this HttpRequestMessage httpRequest, HttpRequestOptions requestOptions)
        {
            if (requestOptions == null)
                return httpRequest;

            // Apply the ETag header.
            ApplyETag(httpRequest, requestOptions.ETag);

            // Apply updates to the query string.
            var queryString = QueryString.FromUriComponent(httpRequest.RequestUri);
            queryString = requestOptions.AddToQueryString(queryString);
            httpRequest.RequestUri = new UriBuilder(httpRequest.RequestUri) { Query = queryString.ToUriComponent() }.Uri;

            return httpRequest;
        }

        /// <summary>
        /// Applies the <i>ETag</i> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.Headers"/> as an <see cref="HttpRequestHeader.IfNoneMatch"/> (where <see cref="HttpRequest.Method"/> is <see cref="HttpMethod.Get"/>
        /// or <see cref="HttpMethod.Head"/>); otherwise, an <see cref="HttpRequestHeader.IfMatch"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="etag">The <i>ETag</i> value.</param>
        /// <returns>The <see cref="HttpRequestMessage"/> to support fluent-style method-chaining.</returns>
        public static HttpRequestMessage ApplyETag(this HttpRequestMessage httpRequest, string? etag)
        {
            // Apply the ETag header.
            if (!string.IsNullOrEmpty(etag))
            {
                if (httpRequest.Method == HttpMethod.Get || httpRequest.Method == HttpMethod.Head)
                    httpRequest.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(etag));
                else
                    httpRequest.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(etag));
            }

            return httpRequest;
        }

        /// <summary>
        /// Deserialize the HTTP JSON <see cref="HttpRequest.Body"/> to a specified .NET object <see cref="Type"/> via a <see cref="HttpRequestJsonValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <returns>The <see cref="HttpRequestJsonValue{T}"/>.</returns>
        public static async Task<HttpRequestJsonValue<T>> ReadAsJsonValueAsync<T>(this HttpRequest httpRequest, IJsonSerializer jsonSerializer, bool valueIsRequired = true)
        {
            // Do not close/dispose StreamReader as that will close underlying stream which may cause a further downstream exception.
            var sr = new StreamReader((httpRequest ?? throw new ArgumentNullException(nameof(httpRequest))).Body);
            var json = await sr.ReadToEndAsync();
            var jv = new HttpRequestJsonValue<T>();

            // Deserialize the JSON into the selected type.
            try
            {
                if (!string.IsNullOrEmpty(json))
                    jv.Value = (jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer))).Deserialize<T>(json)!;

                if (valueIsRequired && jv.Value == null)
                    jv.ValidationException = new ValidationException($"{_errorText} Value is mandatory.");
            }
            catch (Exception ex)
            {
                jv.ValidationException = new ValidationException($"{_errorText} {ex.Message}", ex);
            }

            return jv;
        }

        /// <summary>
        /// Deserialize the HTTP JSON <see cref="HttpRequest.Body"/> to a specified .NET object <see cref="Type"/> via a <see cref="HttpRequestJsonValue"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <returns>The <see cref="HttpRequestJsonValue"/>.</returns>
        public static async Task<HttpRequestJsonValue> ReadAsJsonValueAsync(this HttpRequest httpRequest, IJsonSerializer jsonSerializer, bool valueIsRequired = true)
        {
            // Do not close/dispose StreamReader as that will close underlying stream which may cause a further exception.
            var sr = new StreamReader((httpRequest ?? throw new ArgumentNullException(nameof(httpRequest))).Body);
            var json = await sr.ReadToEndAsync();
            var jv = new HttpRequestJsonValue();

            // Deserialize the JSON into the selected type.
            try
            {
                jv.Value = (jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer))).Deserialize(json)!;
                if (valueIsRequired && jv.Value == null)
                    jv.ValidationException = new ValidationException($"{_errorText} Value is mandatory.");
            }
            catch (Exception ex)
            {
                jv.ValidationException = new ValidationException($"{_errorText} {ex.Message}", ex);
            }

            return jv;
        }

        /// <summary>
        /// Gets the <see cref="PagingArgs"/> from the <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="includePaging">Indicates whether to include the get of the <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="HttpRequestOptions"/>.</returns>
        public static HttpRequestOptions GetRequestOptions(this HttpRequest httpRequest, bool includePaging = true)
        {
            var ro = HttpRequestOptions.GetRequestOptions((httpRequest ?? throw new ArgumentNullException(nameof(httpRequest))).Query, includePaging);
            if (httpRequest.Headers != null && httpRequest.Headers.Count > 0)
            {
                if (httpRequest.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var vals) || httpRequest.Headers.TryGetValue(HeaderNames.IfMatch, out vals))
                {
                    var etag = vals.FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(etag))
                        ro.ETag = etag;
                }
            }

            return ro;
        }

        /// <summary>
        /// Gets the <see cref="PagingArgs"/> from the <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <returns>The <see cref="PagingArgs"/> where found; otherwise, <c>null</c>.</returns>
        public static PagingArgs? GetPagingArgs(this HttpRequest httpRequest)
            => HttpRequestOptions.TryGetPagingArgs((httpRequest ?? throw new ArgumentNullException(nameof(httpRequest))).Query, out var paging) ? paging : null;

        /// <summary>
        /// Converts the <see cref="HttpResponseMessage"/> to the equivalent <see cref="IExtendedException"/> based on the <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <returns>The corresponding <see cref="IExtendedException"/> where applicable; otherwise, <c>null</c>.</returns>
        public async static Task<IExtendedException?> ToExtendedExceptionAsync(this HttpResponseMessage response, bool useContentAsErrorMessage = false)
        {
            if (response == null || response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return HttpResult.CreateExtendedException(response, content);
        }

        /// <summary>
        /// Trys to get the first named <see cref="HttpResponseMessage.Headers"/> value from the <paramref name="response"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="name">The header name.</param>
        /// <param name="value">The header value where found.</param>
        /// <returns><c>true</c> where the header value is found; otherwise, <c>false</c>.</returns>
        public static bool TryGetHeaderValue(this HttpResponseMessage response, string name, out string? value)
        {
            value = null;
            if (response == null || response.Headers == null || string.IsNullOrEmpty(name))
                return false;

            if (response.Headers.TryGetValues(name, out IEnumerable<string> values))
            {
                value = values.FirstOrDefault();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Trys to get the <see cref="PagingResult"/> from the <paramref name="response"/> headers.
        /// </summary>
        /// <param name="response">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="result">The <see cref="PagingResult"/> where found.</param>
        /// <returns><c>true</c> where the <see cref="PagingResult"/> is found; otherwise, <c>false</c>.</returns>
        public static bool TryGetPagingResult(this HttpResponseMessage response, out PagingResult? result)
        {
            var skip = HttpUtility.ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingSkipHeaderName, out var vs) ? vs : null);
            var page = skip.HasValue ? null : HttpUtility.ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingPageNumberHeaderName, out var vpn) ? vpn : null);

            if (skip.HasValue)
                result = new PagingResult(PagingArgs.CreateSkipAndTake(skip.Value, HttpUtility.ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingTakeHeaderName, out var vt) ? vt : null)));
            else if (page.HasValue)
                result = new PagingResult(PagingArgs.CreatePageAndSize(page.Value, HttpUtility.ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingPageSizeHeaderName, out var vps) ? vps : null)));
            else
            {
                result = null;
                return false;
            }

            result.TotalCount = HttpUtility.ParseLongValue(TryGetHeaderValue(response, HttpConsts.PagingTotalCountHeaderName, out var vtc) ? vtc : null);
            return true;
        }
    }
}