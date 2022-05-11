// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Json;
using CoreEx.Validation;
using CoreEx.WebApis;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Extension methods for <see cref="HttpRequest"/>.
    /// </summary>
    public static class HttpExtensions
    {
        /// <summary>
        /// Gets the standard invalid JSON message prefix.
        /// </summary>
        public const string InvalidJsonMessagePrefix = "Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted:";

        /// <summary>
        /// Applies the <see cref="HttpRequestOptions"/> to the <see cref="HttpRequestMessage"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="requestOptions">The <see cref="HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequestMessage"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This will automatically invoke <see cref="ApplyETag(HttpRequestMessage, string)"/> where there is an <see cref="HttpRequestOptions.ETag"/> value.</remarks>
        public static HttpRequestMessage ApplyRequestOptions(this HttpRequestMessage httpRequest, HttpRequestOptions requestOptions)
        {
            if (httpRequest == null)
                throw new ArgumentNullException(nameof(httpRequest));

            if (requestOptions == null)
                return httpRequest;

            // Apply the ETag header.
            ApplyETag(httpRequest, requestOptions.ETag);

            // Apply updates to the query string.
            var queryString = QueryString.FromUriComponent(httpRequest.RequestUri);
            queryString = requestOptions.AddToQueryString(queryString);
            var ub = httpRequest.RequestUri == null ? new UriBuilder() : new UriBuilder(httpRequest.RequestUri);
            ub.Query = queryString.ToUriComponent();
            httpRequest.RequestUri = ub.Uri;

            return httpRequest;
        }

        /// <summary>
        /// Applies the <i>ETag</i> to the <see cref="HttpRequestMessage"/> <see cref="HttpRequestMessage.Headers"/> as an <see cref="HttpRequestHeader.IfNoneMatch"/> (where <see cref="HttpRequest.Method"/> is <see cref="HttpMethod.Get"/>
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
                    httpRequest.Headers.IfNoneMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(FormatETag(etag)));
                else
                    httpRequest.Headers.IfMatch.Add(new System.Net.Http.Headers.EntityTagHeaderValue(FormatETag(etag)));
            }

            return httpRequest;
        }

        /// <summary>
        /// Formats the ETag to be compliant.
        /// </summary>
        private static string FormatETag(string etag) => etag.StartsWith('\"') && etag.EndsWith('\"') ? etag : $"\"{etag}\"";

        /// <summary>
        /// Applies the <see cref="HttpRequestOptions"/> to the <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="requestOptions">The <see cref="HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequest"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This will automatically invoke <see cref="ApplyETag(HttpRequestMessage, string)"/> where there is an <see cref="HttpRequestOptions.ETag"/> value.</remarks>
        public static HttpRequest ApplyRequestOptions(this HttpRequest httpRequest, HttpRequestOptions requestOptions)
        {
            if (httpRequest == null)
                throw new ArgumentNullException(nameof(httpRequest));

            if (requestOptions == null)
                return httpRequest;

            // Apply the ETag header.
            ApplyETag(httpRequest, requestOptions.ETag);

            // Apply updates to the query string.
            httpRequest.QueryString = requestOptions.AddToQueryString(httpRequest.QueryString);
            return httpRequest;
        }

        /// <summary>
        /// Applies the <i>ETag</i> to the <see cref="HttpRequest"/> <see cref="HttpRequest.Headers"/> as an <see cref="HttpRequestHeader.IfNoneMatch"/> (where <see cref="HttpRequest.Method"/> is <see cref="HttpMethod.Get"/>
        /// or <see cref="HttpMethod.Head"/>); otherwise, an <see cref="HttpRequestHeader.IfMatch"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="etag">The <i>ETag</i> value.</param>
        /// <returns>The <see cref="HttpRequest"/> to support fluent-style method-chaining.</returns>
        /// <remarks>Automatically adds quoting to be ETag Header format compliant.</remarks>
        public static HttpRequest ApplyETag(this HttpRequest httpRequest, string? etag)
        {
            // Apply the ETag header.
            if (!string.IsNullOrEmpty(etag))
            {
                if (httpRequest.Method.Equals(HttpMethod.Get.Method, StringComparison.InvariantCultureIgnoreCase) || httpRequest.Method.Equals(HttpMethod.Head.Method, StringComparison.InvariantCultureIgnoreCase))
                    httpRequest.Headers.Add(HeaderNames.IfNoneMatch, FormatETag(etag));
                else
                    httpRequest.Headers.Add(HeaderNames.IfMatch, FormatETag(etag));
            }

            return httpRequest;
        }

        /// <summary>
        /// Deserialize the HTTP JSON <see cref="HttpRequest.Body"/> to a specified .NET object <see cref="Type"/> via a <see cref="HttpRequestJsonValue{T}"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where <c>null</c>.</param>
        /// <param name="validator">The optional <see cref="IValidator{T}"/> to validate the value (only invoked where the value is not <c>null</c>).</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="HttpRequestJsonValue{T}"/>.</returns>
        public static async Task<HttpRequestJsonValue<T>> ReadAsJsonValueAsync<T>(this HttpRequest httpRequest, IJsonSerializer jsonSerializer, bool valueIsRequired = true, IValidator<T>? validator = null, CancellationToken cancellationToken = default)
        {
            using var sr = new StreamReader((httpRequest ?? throw new ArgumentNullException(nameof(httpRequest))).Body, Encoding.UTF8, true, 1024, leaveOpen: true);
            var json = await sr.ReadToEndAsync().ConfigureAwait(false);
            var jv = new HttpRequestJsonValue<T>();

            // Deserialize the JSON into the selected type.
            try
            {
                if (!string.IsNullOrEmpty(json)) 
                    jv.Value = (jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer))).Deserialize<T>(json)!;

                if (valueIsRequired && jv.Value == null)
                    jv.ValidationException = new ValidationException($"{InvalidJsonMessagePrefix} Value is mandatory.");

                if (jv.Value != null && validator != null)
                {
                    var vr = await validator.ValidateAsync(jv.Value, cancellationToken).ConfigureAwait(false);
                    jv.ValidationException = vr.ToValidationException();
                }
            }
            catch (Exception ex)
            {
                jv.ValidationException = new ValidationException($"{InvalidJsonMessagePrefix} {ex.Message}", ex);
            }

            return jv;
        }


        /// <summary>
        /// Reads the HTTP <see cref="HttpRequest.Body"/> as a <see cref="string"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where null.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The content where successful, otherwise the <see cref="ValidationException"/> invalid.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "StreamReader.ReadToEndAsync does not currently support, it is there in case it ever does.")]
        public static async Task<(string? Content, ValidationException? Exception)> ReadAsStringAsync(this HttpRequest httpRequest, bool valueIsRequired = true, CancellationToken cancellationToken = default)
        {
            using var sr = new StreamReader((httpRequest ?? throw new ArgumentNullException(nameof(httpRequest))).Body, Encoding.UTF8, true, 1024, leaveOpen: true);
            var content = await sr.ReadToEndAsync().ConfigureAwait(false);
            if (valueIsRequired && string.IsNullOrEmpty(content))
                return (null, new ValidationException($"{InvalidJsonMessagePrefix} Value is mandatory."));
            else
                return (content, null);
        }

        /// <summary>
        /// Gets the <see cref="WebApiRequestOptions"/> from the <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <returns>The <see cref="HttpRequestOptions"/>.</returns>
        public static WebApiRequestOptions GetRequestOptions(this HttpRequest httpRequest)
            => new(httpRequest ?? throw new ArgumentNullException(nameof(httpRequest)));

        /// <summary>
        /// Adds the <see cref="PagingArgs"/> to the <see cref="HttpResponse"/>.
        /// </summary>
        /// <param name="httpResponse">The <see cref="HttpResponse"/>.</param>
        /// <param name="paging">The <see cref="PagingResult"/>.</param>
        public static void AddPagingResult(this HttpResponse httpResponse, PagingResult? paging)
            => httpResponse.Headers.AddPagingResult(paging);

        /// <summary>
        /// Adds the <see cref="PagingArgs"/> to the <see cref="IHeaderDictionary"/>.
        /// </summary>
        /// <param name="headers">The <see cref="IHeaderDictionary"/>.</param>
        /// <param name="paging">The <see cref="PagingResult"/>.</param>
        public static void AddPagingResult(this IHeaderDictionary headers, PagingResult? paging)
        {
            if (paging == null)
                return;

            if (paging.IsSkipTake)
            {
                headers[HttpConsts.PagingSkipHeaderName] = paging.Skip.ToString(CultureInfo.InvariantCulture);
                headers[HttpConsts.PagingTakeHeaderName] = paging.Take.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                headers[HttpConsts.PagingPageNumberHeaderName] = paging.Page!.Value.ToString(CultureInfo.InvariantCulture);
                headers[HttpConsts.PagingPageSizeHeaderName] = paging.Take.ToString(CultureInfo.InvariantCulture);
            }

            if (paging.TotalCount.HasValue)
                headers[HttpConsts.PagingTotalCountHeaderName] = paging.TotalCount.Value.ToString(CultureInfo.InvariantCulture);

            if (paging.TotalPages.HasValue)
                headers[HttpConsts.PagingTotalPagesHeaderName] = paging.TotalPages.Value.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Converts the <see cref="HttpResponseMessage"/> to the equivalent <see cref="IExtendedException"/> based on the <see cref="HttpStatusCode"/>.
        /// </summary>
        /// <param name="response">The <see cref="HttpResponseMessage"/>.</param>
        /// <param name="useContentAsErrorMessage">Indicates whether to use the <see cref="HttpResponseMessage.Content"/> as the resulting exception message.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The corresponding <see cref="IExtendedException"/> where applicable; otherwise, <c>null</c>.</returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "StreamReader.ReadToEndAsync does not currently support, it is there is case it ever does.")]
        public async static Task<IExtendedException?> ToExtendedExceptionAsync(this HttpResponseMessage response, bool useContentAsErrorMessage = false, CancellationToken cancellationToken = default)
        {
            if (response == null || response.IsSuccessStatusCode)
                return null;

            var content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return HttpResult.CreateExtendedException(response, content, useContentAsErrorMessage);
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

            if (response.Headers.TryGetValues(name, out IEnumerable<string>? values))
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