// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Database;
using CoreEx.Database.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extension methods.
    /// </summary>
    public static class DatabaseServiceCollectionExtensions
    {
        /// <summary>
        /// Adds an <see cref="IDatabase"/> as a scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="create">The function to create the <see cref="IDatabase"/> instance.</param>
        /// <param name="healthCheck">Indicates whether a corresponding <see cref="DatabaseHealthCheck{TDatabase}"/> should be configured.</param>
        /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
        public static IServiceCollection AddDatabase(this IServiceCollection services, Func<IServiceProvider, IDatabase> create, bool healthCheck = true)
        {
            services.AddScoped(sp => create(sp) ?? throw new InvalidOperationException($"An {nameof(IDatabase)} instance must be instantiated."));
            return AddHealthCheck(services, healthCheck);
        }

        /// <summary>
        /// Adds an <see cref="IDatabase"/> as a scoped service.
        /// </summary>
        /// <typeparam name="TDb">The <see cref="IDatabase"/> <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="healthCheck">Indicates whether a corresponding <see cref="DatabaseHealthCheck{TDatabase}"/> should be configured.</param>
        /// <returns>The <see cref="IServiceCollection"/> to support fluent-style method-chaining.</returns>
        public static IServiceCollection AddDatabase<TDb>(this IServiceCollection services, bool healthCheck = true) where TDb : class, IDatabase
        {
            services.AddScoped<IDatabase, TDb>();
            return AddHealthCheck(services, healthCheck);
        }

        /// <summary>
        /// Adds the <see cref="DatabaseHealthCheck{TDatabase}"/> where configured to do so.
        /// </summary>
        private static IServiceCollection AddHealthCheck(this IServiceCollection services, bool healthCheck)
        {
            if (healthCheck)
                services.AddHealthChecks().AddTypeActivatedCheck<DatabaseHealthCheck<IDatabase>>("Database", HealthStatus.Unhealthy, tags: default!, timeout: TimeSpan.FromSeconds(30));

            return services;
        }
    }
}