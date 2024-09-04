// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.RefData;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Web;

namespace CoreEx.Http
{
    /// <summary>
    /// Represents additional (optional) request options for an <see cref="HttpRequestMessage"/>.
    /// </summary>
    /// <remarks>Usage assumes that the HTTP endpoint supports and actions accordingly; i.e. by sending there is no guarantee that the desired outcome will occur as selected.</remarks>
    public class HttpRequestOptions
    {
        /// <summary>
        /// Creates a new instance of the <see cref="HttpRequestOptions"/> class.
        /// </summary>
        /// <param name="paging">The optional <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="HttpRequestOptions"/>.</returns>
        public static HttpRequestOptions Create(PagingArgs? paging = null) => new() { Paging = paging };

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
        /// Gets or sets the <see cref="Paging"/> <see cref="PagingArgs.Token"/> query string name.
        /// </summary>
        /// <remarks>Defaults to <see cref="HttpConsts.PagingArgsTokenQueryStringName"/>.</remarks>
        public string QueryStringNamePagingArgsToken { get; set; } = HttpConsts.PagingArgsTokenQueryStringName;

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
        public string? ETag { get; set; }

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
            (IncludeFields ??= []).AddRange(fields);
            return this;
        }

        /// <summary>
        /// Appends the <paramref name="fields"/> to the <see cref="ExcludeFields"/>.
        /// </summary>
        /// <param name="fields">The fields to append.</param>
        /// <returns>The current <see cref="HttpRequestOptions"/> instance to support fluent-style method-chaining.</returns>
        public HttpRequestOptions Exclude(params string[] fields)
        {
            (ExcludeFields ??= []).AddRange(fields);
            return this;
        }

        /// <summary>
        /// Updates (overrides) the <see cref="Query"/> <see cref="QueryArgs.Filter"/> using a basic dynamic <i>OData-esque</i> <c>$filter</c> statement.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns>The current <see cref="HttpRequestOptions"/> instance to support fluent-style method-chaining.</returns>
        public HttpRequestOptions Filter(string? filter)
        {
            Query ??= new QueryArgs();
            Query.Filter = filter;
            return this;
        }

        /// <summary>
        /// Updates (overrides) the <see cref="Query"/> <see cref="QueryArgs.OrderBy"/> using a basic dynamic <i>OData-esque</i> <c>$orderby</c> statement.
        /// </summary>
        /// <param name="orderby">The order by.</param>
        /// <returns>The current <see cref="HttpRequestOptions"/> instance to support fluent-style method-chaining.</returns>
        public HttpRequestOptions OrderBy(string orderby)
        {
            Query ??= new QueryArgs();
            Query.OrderBy = orderby;
            return this;
        }

        /// <summary>
        /// Gets or sets the <see cref="PagingArgs"/>.
        /// </summary>
        public PagingArgs? Paging { get; set; }

        /// <summary>
        /// Gets or sets the dynamic <see cref="QueryArgs"/>.
        /// </summary>
        public QueryArgs? Query { get; set; }

        /// <summary>
        /// Gets or sets the optional query string value to include within the <see cref="Uri.Query"/>.
        /// </summary>
        /// <remarks>It is assumed that the contents of these are valid as no encoding will be employed; i.e. will be used as-is. The specification of any leading '<c>&amp;</c>' and '<c>?</c>' characters is not required.</remarks>
        public string? UrlQueryString { get; set; }

        /// <summary>
        /// Indicates whether to include any related texts for the item(s).
        /// </summary>
        /// <remarks>For example, include corresponding <see cref="IReferenceData.Text"/> for any <b>ReferenceData</b> values returned in the JSON response payload.</remarks>
        public bool IncludeText { get; set; }

        /// <summary>
        /// Indicates whether to include any inactive item(s); 
        /// </summary>
        /// <remarks>For example, include item(s) where <see cref="IReferenceData.IsActive"/> is <c>false</c>.</remarks>
        public bool IncludeInactive { get; set; }

        /// <summary>
        /// Adds the <see cref="HttpRequestOptions"/> to a <paramref name="queryString"/>.
        /// </summary>
        /// <param name="queryString">The query string.</param>
        /// <returns>The updated query string.</returns>
        public string? AddToQueryString(string? queryString) => AddToQueryString(string.IsNullOrEmpty(queryString) ? null : HttpUtility.ParseQueryString(queryString));

        /// <summary>
        /// Adds the <see cref="HttpRequestOptions"/> to a <see cref="NameValueCollection"/>.
        /// </summary>
        /// <param name="queryString">The <see cref="NameValueCollection"/>.</param>
        /// <returns>The updated query string.</returns>
        public string? AddToQueryString(NameValueCollection? queryString)
        {
            var sb = new StringBuilder();
            if (queryString is not null)
                AddNameValueCollection(sb, queryString);

            if (Paging is not null)
            {
                switch (Paging.Option)
                {
                    case PagingOption.SkipAndTake:
                        AddNameValuePair(sb, QueryStringNamePagingArgsSkip, Paging.Skip?.ToString(), false);
                        AddNameValuePair(sb, QueryStringNamePagingArgsTake, Paging.Take.ToString(), false);
                        break;

                    case PagingOption.PageAndSize:
                        AddNameValuePair(sb, QueryStringNamePagingArgsPage, Paging.Page?.ToString() ?? 1.ToString(), false);
                        AddNameValuePair(sb, QueryStringNamePagingArgsSize, Paging.Size.ToString(), false);
                        break;

                    default:
                        AddNameValuePair(sb, QueryStringNamePagingArgsToken, Paging.Token, false);
                        AddNameValuePair(sb, QueryStringNamePagingArgsTake, Paging.Take.ToString(), false);
                        break;

                }

                if (Paging.IsGetCount)
                    AddNameValuePair(sb, QueryStringNamePagingArgsCount, "true", false);
            }

            if (Query is not null)
            {
                if (!string.IsNullOrEmpty(Query.Filter))
                    AddNameValuePair(sb, HttpConsts.QueryArgsFilterQueryStringName, Query.Filter, true);

                if (!string.IsNullOrEmpty(Query.OrderBy))
                    AddNameValuePair(sb, HttpConsts.QueryArgsOrderByQueryStringName, Query.OrderBy, true);
            }

            if (IncludeFields != null && IncludeFields.Count > 0)
                AddNameValuePairs(sb, QueryStringNameIncludeFields, IncludeFields.Where(x => !string.IsNullOrEmpty(x)).Select(x => HttpUtility.UrlEncode(x)).ToArray(), false, true);

            if (ExcludeFields != null && ExcludeFields.Count > 0)
                AddNameValuePairs(sb, QueryStringNameExcludeFields, ExcludeFields.Where(x => !string.IsNullOrEmpty(x)).Select(x => HttpUtility.UrlEncode(x)).ToArray(), false, true);

            if (IncludeText)
                AddNameValuePair(sb, QueryStringNameIncludeText, "true", false);

            if (IncludeInactive)
                AddNameValuePair(sb, QueryStringNameIncludeInactive, "true", false);

            var qs = sb.Length == 0 ? null : sb.ToString();
            if (!string.IsNullOrEmpty(UrlQueryString))
            {
                if (qs is null)
                    return UrlQueryString.StartsWith('?') ? UrlQueryString : $"?{(UrlQueryString.StartsWith('&') ? UrlQueryString[1..] : UrlQueryString)}";
                else
                    return $"{qs}{(UrlQueryString.StartsWith('&') ? UrlQueryString : $"&{UrlQueryString}")}";
            }
            else
                return qs;
        }

        /// <summary>
        /// Add the name/value(s) pair(s) to the string builder.
        /// </summary>
        private static void AddNameValueCollection(StringBuilder sb, NameValueCollection nvc)
        {
            foreach (var name in nvc.AllKeys)
            {
                AddNameValuePairs(sb, name, nvc.GetValues(name), true, false);
            }
        }

        /// <summary>
        /// Add the name/value(s) pair to the string builder.
        /// </summary>
        private static void AddNameValuePairs(StringBuilder sb, string? name, string[]? values, bool encode = false, bool concatenateValues = false)
        {
            if (values is null || values.Length == 0)
                return;
            else if (concatenateValues)
                AddNameValuePair(sb, name, string.Join(",", values), encode);
            else
            {
                foreach (var value in values)
                {
                    AddNameValuePair(sb, name, value, encode);
                }
            }
        }

        /// <summary>
        /// Add the name/value pair to the string builder.
        /// </summary>
        private static void AddNameValuePair(StringBuilder sb, string? name, string? value, bool encode = false)
        {
            var nne = string.IsNullOrEmpty(name);
            var vne = string.IsNullOrEmpty(value);

            if (nne && vne)
                return;

            sb.Append(sb.Length == 0 ? '?' : '&');
            if (!nne)
                sb.Append(name);

            if (!vne)
            {
                sb.Append('=');
                sb.Append(encode ? HttpUtility.UrlEncode(value) : value);
            }
        }
    }
}