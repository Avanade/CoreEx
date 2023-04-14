// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.DependencyInjection;
using System;
using FV = FluentValidation;

namespace CoreEx.FluentValidation
{
    /// <summary>
    /// Provides access to the fluent-validator capabilities.
    /// </summary>
    public static class FluentValidator
    {
        /// <summary>
        /// Creates (or gets) an instance of the validator.
        /// </summary>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/>; defaults to <see cref="ExecutionContext.ServiceProvider"/> where not specified.</param>
        /// <returns>The <typeparamref name="TValidator"/> instance.</returns>
        public static TValidator Create<TValidator>(IServiceProvider? serviceProvider = null) where TValidator : FV.IValidator
            => (serviceProvider == null ? ExecutionContext.GetService<TValidator>() : serviceProvider.GetService<TValidator>())
                ?? throw new InvalidOperationException($"Attempted to get service '{typeof(TValidator).FullName}' but null was returned; this would indicate that the service has not been configured correctly.");
    }
}