// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class IFluentServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="FluentValidation.IValidator{T}"/> and corresponding <see cref="CoreEx.FluentValidation.ValidatorWrapper{T}"/> as scoped services.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddFluentValidator<T, TValidator>(this IServiceCollection services) where TValidator : class, FluentValidation.IValidator<T>
            => (services ?? throw new ArgumentNullException(nameof(services)))
                .AddScoped<FluentValidation.IValidator<T>, TValidator>()
                .AddScoped<CoreEx.Validation.IValidator<T>>(sp => new CoreEx.FluentValidation.ValidatorWrapper<T>(sp.GetRequiredService<FluentValidation.IValidator<T>>()));

        /// <summary>
        /// Adds all the <see cref="FluentValidation.IValidator{T}"/>(s) and corresponding <see cref="CoreEx.FluentValidation.ValidatorWrapper{T}"/>(s) from the specified <typeparamref name="TAssembly"/> as scoped services.
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="includeInternalTypes">Indicates whether to include internally defined types.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddFluentValidators<TAssembly>(this IServiceCollection services, bool includeInternalTypes = false)
        {
            var afv = typeof(IFluentServiceCollectionExtensions).GetMethod(nameof(AddFluentValidator), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);

            foreach (var match in from type in includeInternalTypes ? typeof(TAssembly).Assembly.GetTypes() : typeof(TAssembly).Assembly.GetExportedTypes()
                                  where !type.IsAbstract && !type.IsGenericTypeDefinition
                                  let interfaces = type.GetInterfaces()
                                  let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(FluentValidation.IValidator<>))
                                  let @interface = genericInterfaces.FirstOrDefault()
                                  let valueType = @interface?.GetGenericArguments().FirstOrDefault()
                                  where @interface != null
                                  select new { valueType, type })
            {
                afv.MakeGenericMethod(match.valueType, match.type).Invoke(null, new object[] { services });
            }

            return services;
        }
    }
}