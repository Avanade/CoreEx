using System;
using System.Threading;
using System.Threading.Tasks;
using CoreEx.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CoreEx.Healthchecks.Checks
{

    /// <summary> Health check for Typed Http clients inheriting from <see cref="TypedHttpClientCore{T}"/>. </summary>
    public class TypedHttpClientCoreHealthCheck<T> : IHealthCheck
        where T : TypedHttpClientCore<T>
    {
        private readonly T _client;

        /// <summary> constructor. </summary>
        public TypedHttpClientCoreHealthCheck(T client)
        {
            _client = client;
        }

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (_client == null)
            {
                return HealthCheckResult.Unhealthy($"Typed Http client dependency for '{typeof(T)}' not resolved");
            }

            try
            {
                var result = await _client.HealthCheckAsync();

                result.Response.EnsureSuccessStatusCode();

                return HealthCheckResult.Healthy();
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, exception: ex);
            }
        }
    }
}