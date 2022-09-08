// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.HealthChecks
{
    /// <summary>
    /// Health check for Typed Http clients inheriting from <see cref="TypedHttpClientCore{T}"/>.
    /// </summary>
    public class TypedHttpClientCoreHealthCheck<T> : IHealthCheck where T : TypedHttpClientCore<T>
    {
        private readonly T? _client;
        private readonly IReadOnlyDictionary<string, object> _data;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypedHttpClientCoreHealthCheck{T}"/> class.
        /// </summary>
        public TypedHttpClientCoreHealthCheck(T client)
        {
            _client = client;
            _data = new Dictionary<string, object>
            {
                { "host", _client?.BaseAddress?.Host ?? "unknown" }
            };
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_client == null)
                return HealthCheckResult.Unhealthy($"Typed Http client dependency for '{typeof(T)}' not resolved", data: _data);

            try
            {
                var result = await _client.HealthCheckAsync(cancellationToken);
                result.Response.EnsureSuccessStatusCode();
                return HealthCheckResult.Healthy(data: _data);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex, data: _data);
            }
        }
    }
}