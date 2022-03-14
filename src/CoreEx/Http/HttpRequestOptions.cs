﻿// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents additional (optional) request options for an <see cref="HttpRequestOptions"/>.
    /// </summary>
    /// <remarks>Usage assumes that the HTTP endpoint supports and actions accordingly; i.e. by sending there is no guarantee that the desired outcome will occur as selected.</remarks>
    public class HttpRequestOptions : IETag
    {
        private string? _etag;

        /// <summary>
        /// Gets or sets the <see cref="IncludeFields"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.IncludeFieldsQueryStringName"/>.</remarks>
        public string QueryStringNameIncludeFields { get; set; } = HttpConsts.IncludeFieldsQueryStringName;

        /// <summary>
        /// Gets or sets the <see cref="ExcludeFields"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.ExcludeFieldsQueryStringName"/>.</remarks>
        public string QueryStringNameExcludeFields { get; set; } = HttpConsts.ExcludeFieldsQueryStringName;

        /// <summary>
        /// Gets or sets the <see cref="Paging"/> <see cref="PagingArgs.Page"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.PagingArgsPageQueryStringName"/>.</remarks>
        public string QueryStringNamePagingArgsPage { get; set; } = HttpConsts.PagingArgsPageQueryStringName;

        /// <summary>
        /// Gets or sets the <see cref="Paging"/> <see cref="PagingArgs.Size"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.PagingArgsSizeQueryStringName"/>.</remarks>
        public string QueryStringNamePagingArgsSize { get; set; } = HttpConsts.PagingArgsSizeQueryStringName;

        /// <summary>
        /// Gets or sets the <see cref="Paging"/> <see cref="PagingArgs.Skip"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.PagingArgsSkipQueryStringName"/>.</remarks>
        public string QueryStringNamePagingArgsSkip { get; set; } = HttpConsts.PagingArgsSkipQueryStringName;

        /// <summary>
        /// Gets or sets the <see cref="Paging"/> <see cref="PagingArgs.Take"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.PagingArgsTakeQueryStringName"/>.</remarks>
        public string QueryStringNamePagingArgsTake { get; set; } = HttpConsts.PagingArgsTakeQueryStringName;

        /// <summary>
        /// Gets or sets the <see cref="Paging"/> <see cref="PagingArgs.IsGetCount"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.PagingArgsCountQueryStringName"/>.</remarks>
        public string QueryStringNamePagingArgsCount { get; set; } = HttpConsts.PagingArgsCountQueryStringName;

        /// <summary>
        /// Gets or sets the <see cref="IncludeText"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.IncludeTextQueryStringName"/>.</remarks>
        public string QueryStringNameIncludeText { get; set; } = HttpConsts.IncludeTextQueryStringName;

        /// <summary>
        /// Gets or sets the <see cref="IncludeInactive"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.IncludeInactiveQueryStringName"/>.</remarks>
        public string QueryStringNameIncludeInactive { get; set; } = HttpConsts.IncludeInactiveQueryStringName;

        /// <summary>
        /// Gets or sets the entity tag that will be passed as either a <c>If-None-Match</c> header where <see cref="HttpMethod.Get"/>; otherwise, an <c>If-Match</c> header.
        /// </summary>
        /// <remarks>Automatically adds quoting to be ETag format compliant.</remarks>
        public string? ETag
        {
            get => _etag;
            set => _etag = value == null ? null : (value.StartsWith('\"') && value.StartsWith('\"') ? value : $"\"{value}\"");
        }

        /// <summary>
        /// Gets or sets the list of <b>included</b> fields (JSON property names) to limit the serialized data payload (results in url query string: "$fields=x,y,z").
        /// </summary>
        public List<string>? IncludeFields { get; set; }

        /// <summary>
        /// Gets or sets the list of <b>excluded</b> fields (JSON property names) to limit the serialized data payload (results in url query string: "$excludefields=x,y,z").
        /// </summary>
        public List<string>? ExcludeFields { get; set; }

        /// <summary>
        /// Appends the <paramref name="fields"/> to the <see cref="IncludeFields"/>.
        /// </summary>
        /// <param name="fields">The fields to append.</param>
        /// <returns>The current <see cref="HttpRequestOptions"/> instance to support fluent-style method-chaining.</returns>
        public HttpRequestOptions Include(params string[] fields)
        {
            (IncludeFields ??= new List<string>()).AddRange(fields);
            return this;
        }

        /// <summary>
        /// Appends the <paramref name="fields"/> to the <see cref="ExcludeFields"/>.
        /// </summary>
        /// <param name="fields">The fields to append.</param>
        /// <returns>The current <see cref="HttpRequestOptions"/> instance to support fluent-style method-chaining.</returns>
        public HttpRequestOptions Exclude(params string[] fields)
        {
            (ExcludeFields ??= new List<string>()).AddRange(fields);
            return this;
        }

        /// <summary>
        /// Gets or sets the <see cref="PagingArgs"/>.
        /// </summary>
        public PagingArgs? Paging { get; set; }

        /// <summary>
        /// Gets or sets the optional query string value to include within the <see cref="Uri.Query"/>.
        /// </summary>
        public string? UrlQueryString { get; set; }

        /// <summary>
        /// Indicates whether to include any related texts for the item(s).
        /// </summary>
        /// <remarks>For example, include corresponding <see cref="IReferenceData.Text"/> for any <b>ReferenceData</b> values returned in the JSON response payload.</remarks>
        public bool IncludeText { get; set; }

        /// <summary>
        /// Indicates whether to include any inactive item(s); 
        /// </summary>
        /// <remarks>For example, include item(s) where <see cref="IReferenceDataExtended.IsActive"/> is <c>false</c>.</remarks>
        public bool IncludeInactive { get; set; }

        /// <summary>
        /// Adds the <see cref="HttpRequestOptions"/> to a <see cref="QueryString"/>.
        /// </summary>
        /// <param name="queryString">The input <see cref="QueryString"/>.</param>
        /// <returns>The resulting <see cref="QueryString"/>.</returns>
        public QueryString AddToQueryString(QueryString queryString)
        {
            if (Paging != null)
            {
                if (Paging.IsSkipTake)
                {
                    queryString = queryString.Add(QueryStringNamePagingArgsSkip, Paging.Skip.ToString());
                    queryString = queryString.Add(QueryStringNamePagingArgsTake, Paging.Take.ToString());
                }
                else
                {
                    queryString = queryString.Add(QueryStringNamePagingArgsPage, Paging.Page.ToString());
                    queryString = queryString.Add(QueryStringNamePagingArgsSize, Paging.Size.ToString());
                }

                if (Paging.IsGetCount)
                    queryString = queryString.Add(QueryStringNamePagingArgsCount, "true");
            }

            if (IncludeFields != null && IncludeFields.Count > 0)
                queryString = queryString.Add(QueryStringNameIncludeFields, string.Join(",", IncludeFields.Where(x => !string.IsNullOrEmpty(x))));

            if (ExcludeFields != null && ExcludeFields.Count > 0)
                queryString = queryString.Add(QueryStringNameExcludeFields, string.Join(",", ExcludeFields.Where(x => !string.IsNullOrEmpty(x))));

            if (IncludeText)
                queryString = queryString.Add(QueryStringNameIncludeText, "true");

            if (IncludeInactive)
                queryString = queryString.Add(QueryStringNameIncludeInactive, "true");

            if (!string.IsNullOrEmpty(UrlQueryString))
            {
                var url = UrlQueryString.StartsWith("&") ? UrlQueryString[1..] : UrlQueryString;
                url = url.StartsWith("?") ? url : '?' + url;
                queryString = queryString.Add(QueryString.FromUriComponent(url));
            }

            return queryString;
        }

        /// <summary>
        /// Gets the <see cref="HttpRequestOptions"/> from an <see cref="IQueryCollection"/>.
        /// </summary>
        /// <param name="query">The <see cref="IQueryCollection"/>.</param>
        /// <param name="includePaging">Indicates whether to include the get of the <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="HttpRequestOptions"/>.</returns>
        /// <remarks>Excludes <see cref="ETag"/> as it is an <see cref="HttpRequest.Headers"/> value versus part of the query string; see <see cref="HttpExtensions.GetRequestOptions(HttpRequest, bool)"/> which will include.</remarks>
        public static HttpRequestOptions GetRequestOptions(IQueryCollection query, bool includePaging = true)
        {
            var ro = new HttpRequestOptions();
            if (query == null || query.Count == 0)
                return ro;

            if (includePaging && TryGetPagingArgs(query, out var paging))
                ro.Paging = paging;

            var fields = GetNamedQueryString(query, HttpConsts.IncludeFieldsQueryStringNames);
            if (!string.IsNullOrEmpty(fields))
                ro.Include(fields.Split(',', StringSplitOptions.RemoveEmptyEntries));

            fields = GetNamedQueryString(query, HttpConsts.ExcludeFieldsQueryStringNames);
            if (!string.IsNullOrEmpty(fields))
                ro.Exclude(fields.Split(',', StringSplitOptions.RemoveEmptyEntries));

            ro.IncludeText = HttpUtility.ParseBoolValue(GetNamedQueryString(query, HttpConsts.IncludeTextQueryStringNames));
            ro.IncludeInactive = HttpUtility.ParseBoolValue(GetNamedQueryString(query, HttpConsts.IncludeInactiveQueryStringNames));

            return ro;
        }

        /// <summary>
        /// Gets the <see cref="PagingArgs"/> from an <see cref="IQueryCollection"/>.
        /// </summary>
        /// <param name="query">The <see cref="IQueryCollection"/>.</param>
        /// <param name="paging">The <see cref="PagingArgs"/> where found.</param>
        /// <returns><c>true</c> indicates that the <see cref="PagingArgs"/> where found; otherwise, <c>false</c>.</returns>
        public static bool TryGetPagingArgs(IQueryCollection query, out PagingArgs? paging)
        {
            paging = null;
            if (query == null || query.Count == 0)
                return false;

            long? skip = HttpUtility.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsSkipQueryStringNames));
            long? take = HttpUtility.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsTakeQueryStringNames));
            long? page = skip.HasValue ? null : HttpUtility.ParseLongValue(GetNamedQueryString(query, HttpConsts.PagingArgsPageQueryStringNames));
            bool isGetCount = HttpUtility.ParseBoolValue(GetNamedQueryString(query, HttpConsts.PagingArgsCountQueryStringNames));

            if (skip == null && take == null && page == null && !isGetCount)
                return false;

            if (skip == null && page == null)
                paging = (take.HasValue) ? PagingArgs.CreateSkipAndTake(0, take) : new PagingArgs();
            else
                paging = (skip.HasValue) ? PagingArgs.CreateSkipAndTake(skip.Value, take) : PagingArgs.CreatePageAndSize(page == null ? 0 : page.Value, take);

            paging.IsGetCount = isGetCount;
            return true;
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