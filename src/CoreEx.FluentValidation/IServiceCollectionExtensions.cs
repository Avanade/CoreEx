// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class IServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="FluentValidation.IValidator{T}"/> and corresponding <see cref="CoreEx.FluentValidation.ValidatorWrapper{T}"/> as scoped services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        /// <remarks>Adds the <typeparamref name="TValidator"/> as a scoped </remarks>
        public static IServiceCollection AddFluentValidator<T, TValidator>(this IServiceCollection services) where TValidator : class, FluentValidation.IValidator<T>
            => (services ?? throw new ArgumentNullException(nameof(services)))
                .AddScoped<FluentValidation.IValidator<T>, TValidator>()
                .AddScoped<CoreEx.Validation.IValidator<T>>(sp => new CoreEx.FluentValidation.ValidatorWrapper<T>(sp.GetRequiredService<FluentValidation.IValidator<T>>()));
    }
}