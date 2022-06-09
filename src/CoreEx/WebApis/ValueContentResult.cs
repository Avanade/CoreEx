// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Text;
using System.Threading.Tasks;

namespace CoreEx.WebApis
{
    /// <summary>
    /// Represents a <see cref="ContentResult"/> with a JSON serialized value.
    /// </summary>
    /// <remarks>This contains extended functionality to manage the setting of response headers related to <see cref="ETag"/>, <see cref="PagingResult"/> and <see cref="Location"/>.</remarks>
    public sealed class ValueContentResult : ExtendedContentResult
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
        }

        /// <summary>
        /// Gets or sets the <see cref="IETag.ETag"/> value.
        /// </summary>
        public string? ETag { get; set; }

        /// <summary>
        /// Gets or sets the corresponding <see cref="Entities.PagingResult"/> (where the originating value was an <see cref="ICollectionResult"/>).
        /// </summary>
        public PagingResult? PagingResult { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.
        /// </summary>
        public Uri? Location { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.AddPagingResult(PagingResult);

            var headers = context.HttpContext.Response.GetTypedHeaders();
            if (ETag != null)
                headers.ETag = new EntityTagHeaderValue(ETag.StartsWith('\"') && ETag.EndsWith('\"') ? ETag : $"\"{ETag}\"");

            if (Location != null)
                headers.Location = Location;

            return base.ExecuteResultAsync(context);
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
        /// <param name="location">The <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.</param>
        /// <returns>The <see cref="IActionResult"/>.</returns>
        public static IActionResult CreateResult<T>(T value, HttpStatusCode statusCode, HttpStatusCode? alternateStatusCode, IJsonSerializer jsonSerializer, WebApiRequestOptions requestOptions, bool checkForNotModified, Uri? location)
            => TryCreateValueContentResult(value, statusCode, alternateStatusCode, jsonSerializer, requestOptions, checkForNotModified, location, out var vcr, out var ar) ? vcr! : ar!;

        /// <summary>
        /// Try and create a <see cref="ValueContentResult"/>; otherwise, a <see cref="StatusCodeResult"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="statusCode">The primary status code where there is a value.</param>
        /// <param name="alternateStatusCode">The alternate status code where there is not a value (i.e. <c>null</c>).</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        /// <param name="checkForNotModified">Indicates whether to check for <see cref="HttpStatusCode.NotModified"/> by comparing request and response <see cref="IETag.ETag"/> values.</param>
        /// <param name="location">The <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.</param>
        /// <param name="valueContentResult">The <see cref="ValueContentResult"/> where created.</param>
        /// <param name="alternateResult">The alternate result where <paramref name="valueContentResult"/> not created.</param>
        /// <returns><c>true</c> indicates that the <paramref name="valueContentResult"/> was created; otherwise, <c>false</c> for <paramref name="alternateResult"/> creation.</returns>
        public static bool TryCreateValueContentResult<T>(T value, HttpStatusCode statusCode, HttpStatusCode? alternateStatusCode, IJsonSerializer jsonSerializer, WebApiRequestOptions requestOptions, bool checkForNotModified, Uri? location, out ValueContentResult? valueContentResult, out StatusCodeResult? alternateResult)
        {
            object? val;
            PagingResult? paging;

            // Special case when ICollectionResult, as it is the Result only that is serialized and returned.
            if (value is ICollectionResult cr)
            {
                val = cr.Collection ?? Array.Empty<object?>(); // Where there is an ICollectionResult, then there should always be a value, at least an empty array versus null.
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
                if (alternateStatusCode.HasValue)
                {
                    valueContentResult = null;
                    alternateResult = new StatusCodeResult((int)alternateStatusCode);
                    return false;
                }
                else
                    throw new InvalidOperationException("Function has not returned a result; no AlternateStatusCode has been configured to return.");
            }

            // Where IncludeText is selected then enable before serialization occurs.
            if (requestOptions.IncludeText && ExecutionContext.HasCurrent)
                ExecutionContext.Current.IsTextSerializationEnabled = true;

            // Serialize and generate the etag whilst also applying any filtering of the data where selected.
            string? json = null;
            bool hasETag = TryGetETag(val, out var etag);

            Action<IJsonPreFilterInspector>? inspector;
            if (requestOptions.IncludeFields != null && requestOptions.IncludeFields.Any())
            {
                inspector = hasETag ? null : fi => etag = GenerateETag(requestOptions, val, fi.ToJsonString(), jsonSerializer);
                jsonSerializer.TryApplyFilter(val, requestOptions.IncludeFields, out json, JsonPropertyFilter.Include, preFilterInspector: inspector);
            }
            else if (requestOptions.ExcludeFields != null && requestOptions.ExcludeFields.Any())
            {
                inspector = hasETag ? null : fi => etag = GenerateETag(requestOptions, val, fi.ToJsonString(), jsonSerializer);
                jsonSerializer.TryApplyFilter(val, requestOptions.ExcludeFields, out json, JsonPropertyFilter.Exclude, preFilterInspector: inspector);
            }
            else
            {
                json = jsonSerializer.Serialize(val);
                if (!hasETag)
                    etag = GenerateETag(requestOptions, val, json, jsonSerializer);
            }

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
        /// Determines whether an <see cref="IETag.ETag"/> or <see cref="ExecutionContext.ETag"/> value exists and returns where found.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="etag">The ETag for the value where it exists.</param>
        /// <returns><c>true</c> indicates that the ETag value exists; otherwise, <c>false</c> to generate.</returns>
        internal static bool TryGetETag(object value, [NotNullWhen(true)] out string? etag)
        {
            if (value is IETag ietag && ietag.ETag != null)
            {
                etag = ietag.ETag;
                return true;
            }

            if (ExecutionContext.HasCurrent && ExecutionContext.Current.ETag != null)
            {
                etag = ExecutionContext.Current.ETag;
                return true;
            }

            etag = null;
            return false;
        }

        /// <summary>
        /// Establish the ETag for the value/json.
        /// </summary>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="json">The value serialized to JSON.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <remarks>It is expected that <see cref="TryGetETag(object, out string?)"/> is invoked prior to this to determine whether generation is required.</remarks>
        internal static string GenerateETag(WebApiRequestOptions requestOptions, object value, string? json, IJsonSerializer jsonSerializer)
        {
            if (value is IETag etag && etag.ETag != null)
                return etag.ETag;

            if (ExecutionContext.HasCurrent && ExecutionContext.Current.ETag != null)
                return ExecutionContext.Current.ETag;

            StringBuilder? sb = null;
            if (value is not string && value is IEnumerable coll)
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
                {
                    sb.Clear();
                    sb.Append(json ??= jsonSerializer.Serialize(value));
                }

                // A GET with a collection result should include path and query with the etag.
                if (HttpMethods.IsGet(requestOptions.Request.Method))
                {
                    sb.Append(ETagGenerator.DividerCharacter);

                    if (requestOptions.Request.Path.HasValue)
                        sb.Append(requestOptions.Request.Path.Value);

                    if (requestOptions.Request.QueryString != null)
                        sb.Append(requestOptions.Request.QueryString.ToString());
                }
            }

            // Generate a hash to represent the ETag.
            return ETagGenerator.GenerateHash(sb != null && sb.Length > 0 ? sb.ToString() : json ?? jsonSerializer.Serialize(value));
        }
    }
}