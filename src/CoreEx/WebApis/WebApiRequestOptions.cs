// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Http;
using CoreEx.RefData;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace CoreEx.WebApis
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
            Request = httpRequest ?? throw new ArgumentNullException(nameof(httpRequest));
            GetQueryStringOptions(Request.Query);

            if (httpRequest.Headers != null && httpRequest.Headers.Count > 0)
            {
                if (httpRequest.Headers.TryGetValue(HeaderNames.IfNoneMatch, out var vals) || httpRequest.Headers.TryGetValue(HeaderNames.IfMatch, out vals))
                {
                    var etag = vals.FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(etag))
                        ETag = etag.Trim('\"');
                }
            }
        }

        /// <summary>
        /// Gets the originating <see cref="HttpRequest"/>.
        /// </summary>
        public HttpRequest Request { get; }

        /// <summary>
        /// Gets or sets the entity tag that was passed as either a <c>If-None-Match</c> header where <see cref="HttpMethod.Get"/>; otherwise, an <c>If-Match</c> header.
        /// </summary>
        /// <remarks>Automatically adds quoting to be ETag format compliant.</remarks>
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
        private void GetQueryStringOptions(IQueryCollection query)
        {
            if (query == null || query.Count == 0)
                return;

            var fields = GetNamedQueryString(query, HttpConsts.IncludeFieldsQueryStringNames);
            if (!string.IsNullOrEmpty(fields))
                IncludeFields = fields.Split(',', StringSplitOptions.RemoveEmptyEntries);

            fields = GetNamedQueryString(query, HttpConsts.ExcludeFieldsQueryStringNames);
            if (!string.IsNullOrEmpty(fields))
                ExcludeFields = fields.Split(',', StringSplitOptions.RemoveEmptyEntries);

            IncludeText = HttpUtility.ParseBoolValue(GetNamedQueryString(query, HttpConsts.IncludeTextQueryStringNames));
            IncludeInactive = HttpUtility.ParseBoolValue(GetNamedQueryString(query, HttpConsts.IncludeInactiveQueryStringNames));

            Paging = GetPagingArgs(query);
        }

        /// <summary>
        /// Gets the <see cref="PagingArgs"/> from an <see cref="IQueryCollection"/>.
        /// </summary>
        private static PagingArgs? GetPagingArgs(IQueryCollection query)
        {
            if (query == null || query.Count == 0)
                return null;

            long? skip = HttpUtility.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsSkipQueryStringNames));
            long? take = HttpUtility.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsTakeQueryStringNames));
            long? page = skip.HasValue ? null : HttpUtility.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsPageQueryStringNames));
            bool isGetCount = HttpUtility.ParseBoolValue(GetNamedQueryString(query, HttpConsts.PagingArgsCountQueryStringNames));

            if (skip == null && take == null && page == null && !isGetCount)
                return null;

            PagingArgs paging;
            if (skip == null && page == null)
                paging = (take.HasValue) ? PagingArgs.CreateSkipAndTake(0, take) : new PagingArgs();
            else
                paging = (skip.HasValue) ? PagingArgs.CreateSkipAndTake(skip.Value, take) : PagingArgs.CreatePageAndSize(page == null ? 0 : page.Value, take);

            paging.IsGetCount = isGetCount;
            return paging;
        }

        /// <summary>
        /// Gets the first value for the named query string.
        /// </summary>
        private static string? GetNamedQueryString(IQueryCollection query, IEnumerable<string> names)
        {
            var q = query.Where(x => names.Contains(x.Key, StringComparer.InvariantCultureIgnoreCase)).FirstOrDefault();
            return q.Value.FirstOrDefault();
        }
    }
}