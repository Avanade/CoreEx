// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
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
        private readonly Func<HttpRequestMessage, CancellationToken, Task>? _onBeforeRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientCore{TBase}"/>.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The optional <see cref="IJsonSerializer"/>. Defaults to <see cref="Json.JsonSerializer.Default"/>.</param>
        /// <param name="executionContext">The optional <see cref="ExecutionContext"/>. Defaults to a new instance.</param>
        /// <param name="settings">The optional <see cref="SettingsBase"/>. Defaults to <see cref="DefaultSettings"/>.</param>
        /// <param name="logger">The optional <see cref="ILogger"/>. Defaults to <see cref="NullLogger{T}"/>.</param>
        /// <param name="onBeforeRequest">The optional <see cref="TypedHttpClientBase{TSelf}.OnBeforeRequest(HttpRequestMessage, CancellationToken)"/> function. Defaults to <c>null</c>.</param>
        /// <remarks><see cref="ExecutionContext.GetService{T}"/> is used to default each parameter to a configured service where present before final described defaults.</remarks>
        public TypedHttpClient(HttpClient client, IJsonSerializer? jsonSerializer = null, ExecutionContext? executionContext = null, SettingsBase? settings = null, ILogger<TypedHttpClient>? logger = null, Func<HttpRequestMessage, CancellationToken, Task>? onBeforeRequest = null) : base(client,
            jsonSerializer ?? ExecutionContext.GetService<IJsonSerializer>() ?? Json.JsonSerializer.Default,
            executionContext ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current : new ExecutionContext()),
            settings ?? ExecutionContext.GetService<SettingsBase>() ?? new DefaultSettings(),
            logger ?? ExecutionContext.GetService<ILogger<TypedHttpClient>>() ?? NullLoggerFactory.Instance.CreateLogger<TypedHttpClient>()) 
            => _onBeforeRequest = onBeforeRequest;

        /// <inheritdoc/>
        protected async override Task OnBeforeRequest(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_onBeforeRequest != null)
                await _onBeforeRequest(request, cancellationToken).ConfigureAwait(false);

            await base.OnBeforeRequest(request, cancellationToken).ConfigureAwait(false);
        }
    }
}