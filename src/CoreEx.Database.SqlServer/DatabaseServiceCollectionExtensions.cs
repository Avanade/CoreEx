// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Configuration;
using CoreEx.Database.SqlServer.Outbox;
using CoreEx.Hosting;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extension methods.
    /// </summary>
    public static class DatabaseServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="EventOutboxHostedService"/> using the <see cref="ServiceCollectionHostedServiceExtensions.AddHostedService{THostedService}(IServiceCollection)"/>. 
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="eventOutboxDequeueFactory">The function to create an instance of <see cref="EventOutboxDequeueBase"/> (used to set the underlying <see cref="EventOutboxHostedService.EventOutboxDequeueFactory"/> property).</param>
        /// <param name="partitionKey">The optional partition key.</param>
        /// <param name="destination">The optional destination name (i.e. queue or topic).</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        /// <remarks>To turn off the execution of the <see cref="EventOutboxHostedService"/>(s) at runtime set the '<c>EventOutboxHostedService</c>' configuration setting to <c>false</c>.</remarks>
        public static IServiceCollection AddSqlServerEventOutboxHostedService(this IServiceCollection services, Func<IServiceProvider, EventOutboxDequeueBase> eventOutboxDequeueFactory, string? partitionKey = null, string? destination = null)
        {
            var exe = services.BuildServiceProvider().GetRequiredService<SettingsBase>().GetValue<bool?>("EventOutboxHostedService");
            if (!exe.HasValue || exe.Value)
            {
                services.AddHostedService(sp => new EventOutboxHostedService(sp, sp.GetRequiredService<ILogger<EventOutboxHostedService>>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<IServiceSynchronizer>(), partitionKey, destination)
                {
                    EventOutboxDequeueFactory = eventOutboxDequeueFactory ?? throw new ArgumentNullException(nameof(eventOutboxDequeueFactory))
                });
            }

            return services;
        }
    }
}