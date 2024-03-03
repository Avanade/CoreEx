// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.RefData;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace CoreEx.AspNetCore.WebApis
{
    /// <summary>
    /// Represents the <see cref="WebApi"/> request options; the server-side representation of the <see cref="CoreEx.Http.HttpRequestOptions"/>.
    /// </summary>
    /// <remarks>Usage assumes that the HTTP endpoint supports and actions accordingly; i.e. by sending there is no guarantee that the desired outcome will occur as selected.</remarks>
    public class WebApiRequestOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebApiRequestOptions"/> class.
        /// </summary>
        /// <param name="httpRequest">The <see cref="HttpRequest"/>.</param>
        public WebApiRequestOptions(HttpRequest httpRequest)
        {
            Request = httpRequest.ThrowIfNull(nameof(httpRequest));
            HasQueryString = GetQueryStringOptions(Request.Query);

            // Get the raw ETag from the request headers.
            var rth = httpRequest.GetTypedHeaders();
            var etag = rth.IfNoneMatch.FirstOrDefault()?.Tag ?? rth.IfMatch.FirstOrDefault()?.Tag;
            if (etag.HasValue)
                ETag = etag.Value.Substring(1, etag.Value.Length - 2);
        }

        /// <summary>
        /// Gets the originating <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest Request { get; }

        /// <summary>
        /// Indicates whether the <see cref="Request"/> has a query string.
        /// </summary>
        public bool HasQueryString { get; }

        /// <summary>
        /// Gets or sets the entity tag that was passed as either a <c>If-None-Match</c> header where <see cref="HttpMethod.Get"/>; otherwise, an <c>If-Match</c> header.
        /// </summary>
        /// <remarks>Represents the underlying ray value; i.e. is stripped of any <c>W/"xxxx"</c> formatting.</remarks>
        public string? ETag { get; set; }

        /// <summary>
        /// Gets the list of <b>included</b> fields (JSON property names) to limit the serialized data payload (results in url query string: "$fields=x,y,z").
        /// </summary>
        public string[]? IncludeFields { get; private set; }

        /// <summary>
        /// Gets the list of <b>excluded</b> fields (JSON property names) to limit the serialized data payload (results in url query string: "$excludefields=x,y,z").
        /// </summary>
        public string[]? ExcludeFields { get; private set; }

        /// <summary>
        /// Gets or sets the <see cref="PagingArgs"/>.
        /// </summary>
        public PagingArgs? Paging { get; private set; }

        /// <summary>
        /// Indicates whether to include any related texts for the item(s).
        /// </summary>
        /// <remarks>For example, include corresponding <see cref="IReferenceData.Text"/> for any <b>ReferenceData</b> values returned in the JSON response payload.</remarks>
        public bool IncludeText { get; private set; }

        /// <summary>
        /// Indicates whether to include any inactive item(s); 
        /// </summary>
        /// <remarks>For example, include item(s) where <see cref="IReferenceData.IsActive"/> is <c>false</c>.</remarks>
        public bool IncludeInactive { get; private set; }

        /// <summary>
        /// Gets the options from the <see cref="IQueryCollection"/>.
        /// </summary>
        private bool GetQueryStringOptions(IQueryCollection query)
        {
            if (query == null || query.Count == 0)
                return false;

            var fields = GetNamedQueryString(query, HttpConsts.IncludeFieldsQueryStringNames);
            if (!string.IsNullOrEmpty(fields))
                IncludeFields = fields.Split(',', StringSplitOptions.RemoveEmptyEntries);

            fields = GetNamedQueryString(query, HttpConsts.ExcludeFieldsQueryStringNames);
            if (!string.IsNullOrEmpty(fields))
                ExcludeFields = fields.Split(',', StringSplitOptions.RemoveEmptyEntries);

            IncludeText = HttpExtensions.ParseBoolValue(GetNamedQueryString(query, HttpConsts.IncludeTextQueryStringNames));
            IncludeInactive = HttpExtensions.ParseBoolValue(GetNamedQueryString(query, HttpConsts.IncludeInactiveQueryStringNames, "true"));

            Paging = GetPagingArgs(query);
            return true;
        }

        /// <summary>
        /// Gets the <see cref="PagingArgs"/> from an <see cref="IQueryCollection"/>.
        /// </summary>
        private static PagingArgs? GetPagingArgs(IQueryCollection query)
        {
            if (query == null || query.Count == 0)
                return null;

            long? skip = HttpExtensions.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsSkipQueryStringNames));
            long? take = HttpExtensions.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsTakeQueryStringNames));
            long? page = skip.HasValue ? null : HttpExtensions.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsPageQueryStringNames));
            string? token = GetNamedQueryString(query, HttpConsts.PagingArgsTokenQueryStringNames);
            bool isGetCount = HttpExtensions.ParseBoolValue(GetNamedQueryString(query, HttpConsts.PagingArgsCountQueryStringNames));

            if (skip == null && take == null && page == null && string.IsNullOrEmpty(token) && !isGetCount)
                return null;

            PagingArgs paging;
            if (!string.IsNullOrEmpty(token))
                paging = PagingArgs.CreateTokenAndTake(token, take);
            else if (skip == null && page == null)
                paging = take.HasValue ? PagingArgs.CreateSkipAndTake(0, take) : new PagingArgs();
            else
                paging = skip.HasValue ? PagingArgs.CreateSkipAndTake(skip.Value, take) : PagingArgs.CreatePageAndSize(page == null ? 0 : page.Value, take);

            paging.IsGetCount = isGetCount;
            return paging;
        }

        /// <summary>
        /// Gets the first value for the named query string.
        /// </summary>
        private static string? GetNamedQueryString(IQueryCollection query, IEnumerable<string> names, string? defaultValue = null)
        {
            var q = query.Where(x => names.Contains(x.Key, StringComparer.OrdinalIgnoreCase)).FirstOrDefault();
            if (q.Key == null)
                return null;

            var val = q.Value.FirstOrDefault();
            return string.IsNullOrEmpty(val) ? defaultValue : val;
        }
    }
}