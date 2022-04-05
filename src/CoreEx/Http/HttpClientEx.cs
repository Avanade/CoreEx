// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net.Http;

namespace CoreEx.Http
{
    /// <summary>
    /// Provides a light-weight means to instantiate a <see cref="TypedHttpClientCore{TSelf}"/> directly from an existing <see cref="HttpClient"/>.
    /// </summary>
    public sealed class HttpClientEx : TypedHttpClientCore<HttpClientEx>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HttpClientEx"/> class.
        /// </summary>
        /// <param name="client">The underlying <see cref="HttpClient"/>.</param>
        /// <param name="jsonSerializer">The <see cref="IJsonSerializer"/>. Defaults to <see cref="CoreEx.Json.JsonSerializer.Default"/>.</param>
        /// <param name="executionContext">The <see cref="ExecutionContext"/>. Defaults to a new instance where not specified.</param>
        /// <param name="settings">The <see cref="SettingsBase"/>. Defaults to <see cref="DefaultSettings"/> where not specified.</param>
        /// <param name="logger">The <see cref="ILogger"/> Defaults to <see cref="NullLogger{T}"/> where not specified..</param>
        public HttpClientEx(HttpClient client, IJsonSerializer? jsonSerializer = null, ExecutionContext? executionContext = null, SettingsBase? settings = null, ILogger<HttpClientEx>? logger = null)
            : base(client, jsonSerializer ?? CoreEx.Json.JsonSerializer.Default, executionContext ?? (ExecutionContext.HasCurrent ? ExecutionContext.Current : new ExecutionContext()),
                  settings ?? new DefaultSettings(), NullLoggerFactory.Instance.CreateLogger<HttpClientEx>())
        { }
    }
}