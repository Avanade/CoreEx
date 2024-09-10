// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides <see cref="IHttpArg"/> helpers.
    /// </summary>
    public static class HttpArgs
    {
        /// <summary>
        /// Creates an <see cref="IHttpArg"/> <see cref="IEnumerable{T}"/> from the passed <paramref name="args"/>.
        /// </summary>
        /// <param name="args">The <see cref="IHttpArg"/> arguments.</param>
        /// <returns>The <see cref="IHttpArg"/> <see cref="IEnumerable{T}"/>.</returns>
        public static IEnumerable<IHttpArg> Create(params IHttpArg[] args) => args.AsEnumerable();

        /// <summary>
        /// Includes <paramref name="paging"/> by updating <paramref name="requestOptions"/> <see cref="HttpRequestOptions.Paging"/>.
        /// </summary>
        /// <param name="requestOptions">The <see cref="HttpRequestOptions"/> (can be <c>null</c>).</param>
        /// <param name="paging">The <see cref="PagingArgs"/>.</param>
        /// <returns>The <see cref="HttpRequestOptions"/>.</returns>
        /// <remarks>Will create a new <see cref="HttpRequestOptions"/> where the <paramref name="requestOptions"/> is <c>null</c> and the corresponding <paramref name="paging"/> is not <c>null</c>; otherwise, overrides the 
        /// existing <paramref name="requestOptions"/> <see cref="HttpRequestOptions.Paging"/>.</remarks>
        public static HttpRequestOptions? IncludePaging(this HttpRequestOptions? requestOptions, PagingArgs? paging)
        {
            if (requestOptions == null && paging == null)
                return requestOptions;

            requestOptions ??= new HttpRequestOptions();
            requestOptions.WithPaging(paging);
            return requestOptions;
        }

        /// <summary>
        /// Includes the <paramref name="query"/> by updating <paramref name="requestOptions"/> <see cref="HttpRequestOptions.Query"/>.
        /// </summary>
        /// <param name="requestOptions">The <see cref="HttpRequestOptions"/> (can be <c>null</c>).</param>
        /// <param name="query">The <see cref="QueryArgs"/>.</param>
        /// <returns>The <see cref="HttpRequestOptions"/>.</returns>
        /// <remarks>Will create a new <see cref="HttpRequestOptions"/> where the <paramref name="requestOptions"/> is <c>null</c> and the corresponding <paramref name="query"/> is not <c>null</c>; otherwise, overrides the 
        /// existing <paramref name="requestOptions"/> <see cref="HttpRequestOptions.Query"/>.</remarks>
        public static HttpRequestOptions? IncludeQuery(this HttpRequestOptions? requestOptions, QueryArgs? query)
        {
            if (requestOptions == null && query == null)
                return requestOptions;

            requestOptions ??= new HttpRequestOptions();
            requestOptions.WithQuery(query);
            return requestOptions;
        }
    }
}