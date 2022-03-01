// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.DependencyInjection;
using System;

namespace CoreEx.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
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