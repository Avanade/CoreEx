// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Cosmos.HealthChecks
{
    /// <summary>
    /// Provides a generic <see cref="ICosmosDb"/> <see cref="IHealthCheck"/> to verify that the database is accessible by performing a read operation.
    /// </summary>
    /// <typeparam name="TCosmosDb">The <see cref="ICosmosDb"/> <see cref="Type"/>.</typeparam>
    /// <param name="cosmosDb">The <see cref="ICosmosDb"/> to health check.</param>
    public class CosmosDbHealthCheck<TCosmosDb>(TCosmosDb cosmosDb) : IHealthCheck where TCosmosDb : class, ICosmosDb
    {
        private readonly TCosmosDb _cosmosDb = cosmosDb = cosmosDb.ThrowIfNull(nameof(cosmosDb));

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object> { { "database-id", _cosmosDb.Database.Id } };

            try
            {
                var dr = await _cosmosDb.Database.ReadAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return HealthCheckResult.Healthy(null, data);
            }
            catch (Exception ex)
            {
                return new HealthCheckResult(context.Registration.FailureStatus, $"An unexpected CosmosDB database error has occurred: {ex.Message}", ex, data);
            }
        }
    }
}