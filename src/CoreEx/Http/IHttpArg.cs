// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Enables an HTTP argument that may <see cref="AddToQueryString"/> and/or <see cref="ModifyHttpRequestAsync"/>.
    /// </summary>
    public interface IHttpArg
    {
        /// <summary>
        /// Adds the <see cref="IHttpArg"/> to the <see cref="QueryString"/>.
        /// </summary>
        /// <param name="queryString">The input <see cref="QueryString"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <returns>The updated <see cref="QueryString"/>.</returns>
        QueryString AddToQueryString(QueryString queryString, IJsonSerializer jsonSerializer);

        /// <summary>
        /// Modifies the <see cref="HttpRequestMessage"/> from the <see cref="IHttpArg"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task ModifyHttpRequestAsync(HttpRequestMessage request, IJsonSerializer jsonSerializer, CancellationToken cancellationToken = default);
    }
}