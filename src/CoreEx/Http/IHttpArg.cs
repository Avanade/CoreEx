// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System.Collections.Specialized;
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
        /// Adds the <see cref="IHttpArg"/> to the <paramref name="queryString"/>.
        /// </summary>
        /// <param name="queryString">The <see cref="NameValueCollection"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        void AddToQueryString(NameValueCollection queryString, IJsonSerializer jsonSerializer);

        /// <summary>
        /// Modifies the <see cref="HttpRequestMessage"/> from the <see cref="IHttpArg"/>.
        /// </summary>
        /// <param name="request">The <see cref="HttpRequestMessage"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        Task ModifyHttpRequestAsync(HttpRequestMessage request, IJsonSerializer jsonSerializer, CancellationToken cancellationToken = default);
    }
}