// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.RefData.HealthChecks
{
    /// <summary>
    /// Provides a <see cref="ReferenceDataOrchestrator"/> <see cref="IHealthCheck"/> to report the <see cref="ReferenceDataOrchestrator.Cache"/> statistics.
    /// </summary>
    /// <param name="orchestrator">The <see cref="ReferenceDataOrchestrator"/>.</param>
    public class ReferenceDataOrchestratorHealthCheck(ReferenceDataOrchestrator orchestrator) : IHealthCheck
    {
        private readonly ReferenceDataOrchestrator _orchestrator = orchestrator.ThrowIfNull();

        /// <inheritdoc/>
        /// <remarks>Will always return <see cref="HealthCheckResult.Healthy"/>.</remarks>
        public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object> { { "types", _orchestrator.GetAllTypes().Count() } };

#if NET7_0_OR_GREATER
            data.Add("statistics", _orchestrator.Cache.GetCurrentStatistics() ?? new());
#endif

            return Task.FromResult(HealthCheckResult.Healthy(null, data));
        }
    }
}