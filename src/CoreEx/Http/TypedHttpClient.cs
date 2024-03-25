// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Json;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides a basic typed <see cref="HttpClient"/> implementation (see <see cref="TypedHttpClientCore{TSelf}"/>) that supports <see cref="HttpMethod.Head"/>, <see cref="HttpMethod.Get"/>, <see cref="HttpMethod.Post"/>, <see cref="HttpMethod.Put"/>, <see cref="HttpMethod.Patch"/> and <see cref="HttpMethod.Delete"/>.
    /// </summary>
    public sealed class TypedHttpClient : TypedHttpClientCore<TypedHttpClient>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientCore{TBase}"/>.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>. Defaults to a new instance.</param>
        /// <param name="onBeforeRequest">The optional <see cref="TypedHttpClientBase{TSelf}.OnBeforeRequest(HttpRequestMessage, CancellationToken)"/> function. Defaults to <c>null</c>.</param>
        /// <remarks><see cref="ExecutionContext.GetService{T}"/> is used to default each parameter to a configured service where present before final described defaults.</remarks>
        public TypedHttpClient(HttpClient client, IJsonSerializer? jsonSerializer = null, ExecutionContext? executionContext = null, Func<HttpRequestMessage, CancellationToken, Task>? onBeforeRequest = null) 
            : base(client, jsonSerializer, executionContext) 
            => DefaultOptions.OnBeforeRequest(onBeforeRequest);
    }
}