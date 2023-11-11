// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.AspNetCore.WebApis;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Json;
using CoreEx.Validation;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using HttpRequestOptions = CoreEx.Http.HttpRequestOptions;

namespace CoreEx.AspNetCore.Http
{
    /// <summary>
    /// Extension methods for <see cref="HttpRequest"/>.
    /// </summary>
    public static class HttpResultExtensions
    {
        /// <summary>
        /// Gets the standard invalid JSON message prefix.
        /// </summary>
        public const string InvalidJsonMessagePrefix = "Invalid request: content was not provided, contained invalid JSON, or was incorrectly formatted:";

        /// <summary>
        /// Applies the <see cref="HttpRequestOptions"/> to the <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="requestOptions">The <see cref="HttpRequestOptions"/>.</param>
        /// <returns>The <see cref="HttpRequest"/> to support fluent-style method-chaining.</returns>
        /// <remarks>This will automatically invoke <see cref="ApplyETag(HttpRequest, string)"/> where there is an <see cref="HttpRequestOptions.ETag"/> value.</remarks>
        public static HttpRequest ApplyRequestOptions(this HttpRequest httpRequest, HttpRequestOptions requestOptions)
        {
            if (httpRequest == null)
                throw new ArgumentNullException(nameof(httpRequest));

            if (requestOptions == null)
                return httpRequest;

            // Apply the ETag header.
            httpRequest.ApplyETag(requestOptions.ETag);

            // Apply updates to the query string.
            httpRequest.QueryString = QueryString.FromUriComponent(requestOptions.AddToQueryString(httpRequest.QueryString.ToUriComponent())!);

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
                if (httpRequest.Method.Equals(HttpMethod.Get.Method, StringComparison.OrdinalIgnoreCase) || httpRequest.Method.Equals(HttpMethod.Head.Method, StringComparison.OrdinalIgnoreCase))
                    httpRequest.Headers.Add(HeaderNames.IfNoneMatch, ETagGenerator.FormatETag(etag));
                else
                    httpRequest.Headers.Add(HeaderNames.IfMatch, ETagGenerator.FormatETag(etag));
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
            if (httpRequest == null)
                throw new ArgumentNullException(nameof(httpRequest));

            var content = await BinaryData.FromStreamAsync(httpRequest.Body, cancellationToken).ConfigureAwait(false);
            var jv = new HttpRequestJsonValue<T>();

            // Deserialize the JSON into the selected type.
            try
            {
                if (content.ToMemory().Length > 0)
                    jv.Value = (jsonSerializer ?? throw new ArgumentNullException(nameof(jsonSerializer))).Deserialize<T>(content)!;

                if (valueIsRequired && jv.Value == null)
                    jv.ValidationException = new ValidationException($"{InvalidJsonMessagePrefix} Value is mandatory.");

                if (jv.Value != null && validator != null)
                {
                    var vr = await validator.ValidateAsync(jv.Value, cancellationToken).ConfigureAwait(false);
                    jv.ValidationException = vr.ToException();
                }
            }
            catch (Exception ex)
            {
                jv.ValidationException = new ValidationException($"{InvalidJsonMessagePrefix} {ex.Message}", ex);
            }

            return jv;
        }

        /// <summary>
        /// Reads the HTTP <see cref="HttpRequest.Body"/> as <see cref="BinaryData"/> and optionally validates whether <paramref name="valueIsRequired"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <param name="valueIsRequired">Indicates whether the value is required; will consider invalid where underlying <see cref="Stream"/> length is zero.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        /// <returns>The <see cref="BinaryData"/> content where successful, otherwise the <see cref="ValidationException"/> invalid.</returns>
        public static async Task<(BinaryData? Content, ValidationException? Exception)> ReadAsBinaryDataAsync(this HttpRequest httpRequest, bool valueIsRequired = true, CancellationToken cancellationToken = default)
        {
            var content = await BinaryData.FromStreamAsync(httpRequest.Body, cancellationToken).ConfigureAwait(false);

            if (valueIsRequired && content.ToMemory().Length == 0)
                return (null, new ValidationException($"{InvalidJsonMessagePrefix} Value is mandatory."));
            else
                return (content, null);
        }

        /// <summary>
        /// Gets the <see cref="WebApiRequestOptions"/> from the <see cref="HttpRequest"/>.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        /// <returns>The <see cref="WebApiRequestOptions"/>.</returns>
        public static WebApiRequestOptions GetRequestOptions(this HttpRequest httpRequest) => new(httpRequest ?? throw new ArgumentNullException(nameof(httpRequest)));

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
    }
}