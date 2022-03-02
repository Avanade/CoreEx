// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Json;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using Stj = System.Text.Json;

namespace CoreEx.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Removes all items from the <see cref="IServiceCollection"/> for the specified <typeparamref name="TService"/>.
        /// </summary>
        /// <typeparam name="TService">The service <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns><c>true</c> if item was successfully removed; otherwise, <c>false</c>. Also returns <c>false</c> where item was not found.</returns>
        public static bool Remove<TService>(this IServiceCollection services) where TService : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));

            var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(TService));
            return descriptor != null && services.Remove(descriptor);
        }

        /// <summary>
        /// Adds a scoped service to instantiate a new <see cref="ExecutionContext"/> instance.
        /// </summary>
        /// <param name="serviceCollection">The <see cref="IServiceCollection"/>.</param>
        /// <param name="executionContextFactory">The function to override the creation of the <see cref="ExecutionContext"/> instance.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        /// <remarks>Where the <paramref name="executionContextFactory"/> is <c>null</c>, then the <see cref="ExecutionContext.Create"/> is used to create.</remarks>
        public static IServiceCollection AddExecutionContext(this IServiceCollection serviceCollection, Func<IServiceProvider, ExecutionContext>? executionContextFactory = null)
        {
            return serviceCollection.AddScoped(sp =>
            {
                var ec = executionContextFactory?.Invoke(sp) ?? ExecutionContext.Create?.Invoke() ??
                    throw new InvalidOperationException("Unable to create 'ExecutionContext' instance; either (in order) 'executionContextFactory' resulted in null, or 'ExecutionContext.Create' resulted in null.");

                ec.ServiceProvider = sp;

                ExecutionContext.Reset();
                ExecutionContext.SetCurrent(ec);

                return ec;
            });
        }
    }
}