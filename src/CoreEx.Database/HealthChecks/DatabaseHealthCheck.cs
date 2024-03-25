// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CoreEx.Database.HealthChecks
{
    /// <summary>
    /// Provides a generic <see cref="IDatabase"/> <see cref="IHealthCheck"/> to verify the database is accessible by executing a simple <c>SELECT 1</c> statement.
    /// </summary>
    /// <typeparam name="TDatabase">The <see cref="IDatabase"/> type.</typeparam>
    /// <param name="database">The <see cref="IDatabase"/> to health check.</param>
    public class DatabaseHealthCheck<TDatabase>(TDatabase database) : IHealthCheck where TDatabase : IDatabase
    {
        private readonly IDatabase _database = database.ThrowIfNull(nameof(database));

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var conn = _database.GetConnection();
            var data = new Dictionary<string, object> { { "database", conn.Database ?? "<unknown>" }, { "dataSource", conn.DataSource ?? "<unknown>" } };
            var result = await _database.SqlStatement("SELECT 1").NonQueryWithResultAsync(cancellationToken).ConfigureAwait(false);
            return result.IsSuccess ? HealthCheckResult.Healthy(null, data) : new HealthCheckResult(context.Registration.FailureStatus, $"An unexpected database error has occurred.", result.Error, data);
        }
    }
}