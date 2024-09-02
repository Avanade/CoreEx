// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Cosmos;
using CoreEx.Cosmos.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extension methods.
    /// </summary>
    public static class CosmosDbServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="ICosmosDb"/> as a singleton service.
        /// </summary>
        /// <typeparam name="TCosmosDb">The <see cref="ICosmosDb"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="create">The function to create the <see cref="ICosmosDb"/> instance.</param>
        /// <param name="healthCheck">Indicates whether a corresponding <see cref="CosmosDbHealthCheck{TCosmosDb}"/> should be configured.</param>
        /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
        public static IServiceCollection AddCosmosDb<TCosmosDb>(this IServiceCollection services, Func<IServiceProvider, TCosmosDb> create, bool healthCheck = true) where TCosmosDb : class, ICosmosDb
        {
            services.ThrowIfNull(nameof(services)).AddSingleton(sp => create.ThrowIfNull(nameof(create)).Invoke(sp));
            if (healthCheck)
                services.AddHealthChecks().AddCosmosDbHealthCheck<TCosmosDb>();

            return services;
        }

        /// <summary>
        /// Adds an <see cref="ICosmosDb"/> as a singleton service including a corresponding health check.
        /// </summary>
        /// <typeparam name="TCosmosDb">The <see cref="ICosmosDb"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="create">The function to create the <see cref="ICosmosDb"/> instance.</param>
        /// <param name="healthCheckName">The health check name; defaults to '<c>cosmos-db</c>'.</param>
        /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
        public static IServiceCollection AddCosmosDb<TCosmosDb>(this IServiceCollection services, Func<IServiceProvider, TCosmosDb> create, string? healthCheckName) where TCosmosDb : class, ICosmosDb
        {
            services.ThrowIfNull(nameof(services)).AddSingleton(sp => create.ThrowIfNull(nameof(create)).Invoke(sp));
            services.AddHealthChecks().AddCosmosDbHealthCheck<TCosmosDb>(healthCheckName);
            return services;
        }

        /// <summary>
        /// Adds an <see cref="CosmosDbHealthCheck{TCosmosDb}"/> to verify that the <see cref="ICosmosDb"/> database is accessible by performing a read operation.
        /// </summary>
        /// <typeparam name="TCosmosDb">The <see cref="ICosmosDb"/> <see cref="Type"/>.</typeparam>
        /// <param name="builder">The <see cref="IHealthChecksBuilder"/>.</param>
        /// <param name="healthCheckName">The health check name; defaults to '<c>cosmos-db</c>'.</param>
        /// <returns>The <see cref="IHealthChecksBuilder"/> to support fluent-style method-chaining.</returns>
        public static IHealthChecksBuilder AddCosmosDbHealthCheck<TCosmosDb>(this IHealthChecksBuilder builder, string? healthCheckName = null) where TCosmosDb : class, ICosmosDb
        {
            builder.ThrowIfNull(nameof(builder)).AddTypeActivatedCheck<CosmosDbHealthCheck<TCosmosDb>>(healthCheckName ?? "cosmos-db", HealthStatus.Unhealthy, tags: default!, timeout: TimeSpan.FromSeconds(30));
            return builder;
        }
    }
}