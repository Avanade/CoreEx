﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Abstractions;
using CoreEx.AspNetCore.Http;
using CoreEx.Entities;
using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents an <see cref="ExtendedContentResult"/> with a JSON serialized value.
    /// </summary>
    /// <remarks>This contains extended functionality to manage the setting of response headers related to <see cref="ETag"/>, <see cref="PagingResult"/> and <see cref="Location"/>.
    /// <para>The <see cref="CreateResult{T}"/> and <see cref="TryCreateValueContentResult{T}"/> will return the value as-is where it is an instance of <see cref="IActionResult"/>; i.e. will bypass all related functionality.</para></remarks>
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
        public ValueContentResult(string content, HttpStatusCode statusCode, string? etag, PagingResult? pagingResult, Uri? location)
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

        /// <summary>
        /// Gets or sets the <see cref="TimeSpan"/> for the <see cref="System.Net.Http.Headers.RetryConditionHeaderValue"/>.
        /// </summary>
        public TimeSpan? RetryAfter { get; set; }

        /// <inheritdoc/>
        public override Task ExecuteResultAsync(ActionContext context)
        {
            context.HttpContext.Response.Headers.AddPagingResult(PagingResult);

            var headers = context.HttpContext.Response.GetTypedHeaders();
            if (ETag != null)
                headers.ETag = new EntityTagHeaderValue(ETagGenerator.FormatETag(ETag), true);

            if (Location != null)
                headers.Location = Location;

            if (RetryAfter is not null)
                context.HttpContext.Response.Headers.Append(HeaderNames.RetryAfter, new System.Net.Http.Headers.RetryConditionHeaderValue(RetryAfter.Value).ToString());

            return base.ExecuteResultAsync(context);
        }

        /// <summary>
        /// Creates the <see cref="IActionResult"/> as either <see cref="ValueContentResult"/> or <see cref="StatusCodeResult"/> as per <see cref="TryCreateValueContentResult"/>; unless <paramref name="value"/> is an instance of <see cref="IActionResult"/> which will return as-is.
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
            => TryCreateValueContentResult(value, statusCode, alternateStatusCode, jsonSerializer, requestOptions, checkForNotModified, location, out var pr, out var ar) ? pr! : ar!;

        /// <summary>
        /// Try and create an <see cref="IActionResult"/> as either <see cref="ValueContentResult"/> or <see cref="StatusCodeResult"/> as per <see cref="TryCreateValueContentResult"/>; unless <paramref name="value"/> is an instance of <see cref="IActionResult"/> which will return as-is.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="statusCode">The primary status code where there is a value.</param>
        /// <param name="alternateStatusCode">The alternate status code where there is not a value (i.e. <c>null</c>).</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        /// <param name="checkForNotModified">Indicates whether to check for <see cref="HttpStatusCode.NotModified"/> by comparing request and response <see cref="IETag.ETag"/> values.</param>
        /// <param name="location">The <see cref="Microsoft.AspNetCore.Http.Headers.ResponseHeaders.Location"/> <see cref="Uri"/>.</param>
        /// <param name="primaryResult">The <see cref="IActionResult"/> where created.</param>
        /// <param name="alternateResult">The alternate result where no <paramref name="primaryResult"/>.</param>
        /// <returns><c>true</c> indicates that the <paramref name="primaryResult"/> was created; otherwise, <c>false</c> for <paramref name="alternateResult"/> creation.</returns>
        public static bool TryCreateValueContentResult<T>(T value, HttpStatusCode statusCode, HttpStatusCode? alternateStatusCode, IJsonSerializer jsonSerializer, WebApiRequestOptions requestOptions, bool checkForNotModified, Uri? location, out IActionResult? primaryResult, out IActionResult? alternateResult)
        {
            if (value is Results.IResult)
                throw new ArgumentException($"The {nameof(value)} must not implement {nameof(Results.IResult)}; the underlying {nameof(Results.IResult.Value)} must be unwrapped before invoking.", nameof(value));

            // Where already an IActionResult then return as-is.
            if (value is IActionResult iar)
            {
                primaryResult = iar;
                alternateResult = null;
                return true;
            }

            object? val;
            PagingResult? paging;

            // Special case when ICollectionResult, as it is the Result only that is serialized and returned.
            if (value is ICollectionResult cr)
            {
                val = cr.Items ?? Array.Empty<object?>(); // Where there is an ICollectionResult, then there should always be a value, at least an empty array versus null.
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
                    primaryResult = null;
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

            if (requestOptions.IncludeFields != null && requestOptions.IncludeFields.Length > 0)
                jsonSerializer.TryApplyFilter(val, requestOptions.IncludeFields, out json, JsonPropertyFilter.Include);
            else if (requestOptions.ExcludeFields != null && requestOptions.ExcludeFields.Length > 0)
                jsonSerializer.TryApplyFilter(val, requestOptions.ExcludeFields, out json, JsonPropertyFilter.Exclude);
            else
                json = jsonSerializer.Serialize(val);

            var result = GenerateETag(requestOptions, val, json, jsonSerializer);

            // Check for not-modified and return status accordingly.
            if (checkForNotModified && result.etag == requestOptions.ETag)
            {
                primaryResult = null;
                alternateResult = new StatusCodeResult((int)HttpStatusCode.NotModified);
                return false;
            }

            // Create and return the ValueContentResult.
            primaryResult = new ValueContentResult(result.json!, statusCode, result.etag, paging, location);
            alternateResult = null;
            return true;
        }

        /// <summary>
        /// Establish (use existing or generate) the ETag for the value/json.
        /// </summary>
        /// <param name="requestOptions">The <see cref="WebApiRequestOptions"/>.</param>
        /// <param name="value">The value.</param>
        /// <param name="json">The value serialized to JSON.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <returns>The etag and serialized JSON (where performed).</returns>
        internal static (string? etag, string? json) GenerateETag<T>(WebApiRequestOptions requestOptions, T value, string? json, IJsonSerializer jsonSerializer)
        {
            // Where no query string and there is an etag then that value should be leveraged as the fast-path.
            if (!requestOptions.HasQueryString)
            {
                if (value is IETag etag && etag.ETag != null)
                    return (etag.ETag, json);

                if (ExecutionContext.HasCurrent && ExecutionContext.Current.ResultETag != null)
                    return (ExecutionContext.Current.ResultETag, json);

                // Where there is a collection then we need to generate a hash that represents the collection.
                if (json is null && value is not string && value is IEnumerable coll)
                {
                    var hasEtags = true;
                    var list = new List<string>();

                    foreach (var item in coll)
                    {
                        if (item is IETag cetag && cetag.ETag is not null)
                        {
                            list.Add(cetag.ETag);
                            continue;
                        }

                        // No longer can fast-path as there is no ETag.
                        hasEtags = false;
                        break;
                    }

                    // Where fast-path then return the hash for the etag list.
                    if (hasEtags)
                        return (ETagGenerator.GenerateHash([.. list]), json);
                }
            }

            // Serialize and then generate a hash to represent the etag.
            json ??= jsonSerializer.Serialize(value);
            return (ETagGenerator.GenerateHash(requestOptions.HasQueryString ? [json, requestOptions.Request.QueryString.ToString()] : [json]), json);
        }
    }
}