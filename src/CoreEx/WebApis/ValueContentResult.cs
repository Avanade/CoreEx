// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Json;
using CoreEx.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace CoreEx.WebApis
{
    /// <summary>
    /// Represents a <see cref="ContentResult"/> with a <see cref="Value"/> that will be JSON serialized.
    /// </summary>
    /// <remarks>This contains extended functionality to manage the setting of response headers related to <see cref="ETag"/>, <see cref="PagingResult"/> and <see cref="Location"/>.</remarks>
    public class ValueContentResult : ExtendedContentResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueContentResult"/> class.
        /// </summary>
        /// <param name="content">The value serialized as JSON content.</param>
        /// <param name="statusCode">The <see cref="HttpStatusCode"/>.</param>
        /// <param name="etag">The related <see cref="IETag.ETag"/>.</param>
        /// <param name="pagingResult">The related <see cref="ICollectionResult.Paging"/>.</param>
        /// <param name="location">The <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.</param>
        private ValueContentResult(string content, HttpStatusCode statusCode, string? etag, PagingResult? pagingResult, Uri? location)
        {
            Content = content;
            ContentType = MediaTypeNames.Application.Json;
            StatusCode = (int)statusCode;
            ETag = etag;
            PagingResult = pagingResult;
            Location = location;
            BeforeExtension = ModifyResponse;
        }

        /// <summary>
        /// Gets the value.
        /// </summary>
        public object? Value { get; }

        /// <summary>
        /// Gets the <see cref="IETag.ETag"/> value.
        /// </summary>
        public string? ETag { get; }

        /// <summary>
        /// Gets the corresponding <see cref="Entities.PagingResult"/> (where <see cref="Value"/> is <see cref="ICollectionResult"/>.
        /// </summary>
        public PagingResult? PagingResult { get;}

        /// <summary>
        /// Gets the <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.
        /// </summary>
        public Uri? Location { get; }

        /// <summary>
        /// Modify the response to include the related headers.
        /// </summary>
        private Task ModifyResponse(HttpResponse response)
        {
            response.AddPagingResult(PagingResult);

            var headers = response.GetTypedHeaders();
            if (ETag != null)
                headers.ETag = new EntityTagHeaderValue(ETag.StartsWith('\"') && ETag.EndsWith('\"') ? ETag : $"\"{ETag}\"");

            if (Location != null)
                headers.Location = Location;

            return Task.CompletedTask;
        }

        /// <summary>
        /// Creates the <see cref="IActionResult"/> as either <see cref="ValueContentResult"/> or <see cref="StatusCodeResult"/> as per <see cref="TryCreateValueContentResult"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="statusCode">The primary status code where there is a value.</param>
        /// <param name="alternateStatusCode">The alternate status code where there is not a value (i.e. <c>null</c>).</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        /// <param name="checkForNotModified">Indicates whether to check for <see cref="HttpStatusCode.NotModified"/> by comparing request and response <see cref="IETag.ETag"/> values.</param>
        /// <param name="nullReplacement">The replacement value where <paramref name="value"/> is <c>null</c>.</param>
        /// <param name="location">The  <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/>.</returns>
        public static IActionResult CreateResult<T>(T value, HttpStatusCode statusCode, HttpStatusCode? alternateStatusCode, IJsonSerializer jsonSerializer, WebApiRequestOptions requestOptions, bool checkForNotModified, object? nullReplacement, Uri? location)
            => TryCreateValueContentResult(value, statusCode, alternateStatusCode, jsonSerializer, requestOptions, checkForNotModified, nullReplacement, location, out var vcr, out var ar) ? vcr! : ar!;

        /// <summary>
        /// Try and create a <see cref="ValueContentResult"/>; otherwise, a <see cref="StatusCodeResult"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="statusCode">The primary status code where there is a value.</param>
        /// <param name="alternateStatusCode">The alternate status code where there is not a value (i.e. <c>null</c>).</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        /// <param name="checkForNotModified">Indicates whether to check for <see cref="HttpStatusCode.NotModified"/> by comparing request and response <see cref="IETag.ETag"/> values.</param>
        /// <param name="nullReplacement">The replacement value where <paramref name="value"/> is <c>null</c>.</param>
        /// <param name="location">The  <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.</param>
        /// <param name="valueContentResult">The <see cref="ValueContentResult"/> where created.</param>
        /// <param name="alternateResult">The alternate result where <paramref name="valueContentResult"/> not created.</param>
        /// <returns><c>true</c> indicates that the <paramref name="valueContentResult"/> was created; otherwise, <c>false</c> for <paramref name="alternateResult"/> creation.</returns>
        /// <exception cref="InvalidOperationException"></exception>
        public static bool TryCreateValueContentResult<T>(T value, HttpStatusCode statusCode, HttpStatusCode? alternateStatusCode, IJsonSerializer jsonSerializer, WebApiRequestOptions requestOptions, bool checkForNotModified, object? nullReplacement, Uri? location, out ValueContentResult? valueContentResult, out StatusCodeResult? alternateResult)
        {
            object? val;
            PagingResult? paging;

            // Special case when ICollectionResult, as it is the Result only that is serialized and returned.
            if (value is ICollectionResult cr)
            {
                val = cr.Collection;
                paging = cr.Paging;
            }
            else
            {
                val = value;
                paging = null;
            }

            // Handle null result; generally either not-found, or no-content, depending on context.
            if (val == null)
            {
                if (nullReplacement != null)
                    val = nullReplacement;
                else
                {
                    if (alternateStatusCode.HasValue)
                    {
                        valueContentResult = null;
                        alternateResult = new StatusCodeResult((int)alternateStatusCode);
                        return false;
                    }
                    else
                        throw new InvalidOperationException("Function has not returned a result; no AlternateStatusCode has been configured to return.");
                }
            }

            // Where IncludeText is selected then enable before serialization occurs.
            if (requestOptions.IncludeText && ExecutionContext.HasCurrent)
                ExecutionContext.Current.IsTextSerializationEnabled = true;

            // Serialize whilst also applying any filtering of the data where selected.
            string json;
            if (requestOptions.IncludeFields != null && requestOptions.IncludeFields.Any())
                jsonSerializer.TryApplyFilter(val, requestOptions.IncludeFields, out json, JsonPropertyFilter.Include);
            else if (requestOptions.ExcludeFields != null && requestOptions.ExcludeFields.Any())
                jsonSerializer.TryApplyFilter(val, requestOptions.ExcludeFields, out json, JsonPropertyFilter.Exclude);
            else
                json = jsonSerializer.Serialize(val);

            // Establish an ETag; generate if you have to.
            var etag = EstablishETag(val, json);

            // Check for not-modified and return status accordingly.
            if (checkForNotModified && etag == requestOptions.ETag)
            {
                valueContentResult = null;
                alternateResult = new StatusCodeResult((int)HttpStatusCode.NotModified);
                return false;
            }

            // Create and return the ValueContentResult.
            valueContentResult = new ValueContentResult(json, statusCode, etag, paging, location);
            alternateResult = null;
            return true;
        }

        /// <summary>
        /// Establish the ETag for the value/json.
        /// </summary>
        private static string EstablishETag(object value, string json)
        {
            if (value is IETag etag && etag.ETag != null)
                return etag.ETag;

            if (ExecutionContext.HasCurrent && ExecutionContext.Current.ETag != null)
                return ExecutionContext.Current.ETag;

            StringBuilder? sb = null;
            if (value is IEnumerable coll)
            {
                sb = new StringBuilder();
                var hasEtags = true;

                foreach (var item in coll)
                {
                    if (item is IETag cetag && cetag.ETag != null)
                    {
                        if (sb.Length > 0)
                            sb.Append(ETagGenerator.DividerCharacter);

                        sb.Append(cetag.ETag);
                        continue;
                    }

                    hasEtags = false;
                    break;
                }

                if (!hasEtags)
                    sb = null;
            }

            // Generate a hash to represent the ETag.
            return ETagGenerator.GenerateHash(sb != null && sb.Length > 0 ? sb.ToString() : json);
        }
    }
}