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
        /// Adds the <see cref="FluentValidation.IValidator{T}"/>, the corresponding <see cref="CoreEx.FluentValidation.ValidatorWrapper{T}"/> and <typeparamref name="TValidator"/> as scoped services.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddFluentValidator<T, TValidator>(this IServiceCollection services) where TValidator : class, FluentValidation.IValidator<T>
            => AddFluentValidatorWithInterfacesInternal<T, TValidator>(services);

        /// <summary>
        /// Adds the <see cref="FluentValidation.IValidator{T}"/>, the corresponding <see cref="CoreEx.FluentValidation.ValidatorWrapper{T}"/> and <typeparamref name="TValidator"/> as scoped services.
        /// </summary>
        private static IServiceCollection AddFluentValidatorWithInterfacesInternal<T, TValidator>(this IServiceCollection services) where TValidator : class, FluentValidation.IValidator<T>
            => (services ?? throw new ArgumentNullException(nameof(services)))
                .AddScoped<FluentValidation.IValidator<T>, TValidator>()
                .AddScoped<CoreEx.Validation.IValidator<T>>(sp => new CoreEx.FluentValidation.ValidatorWrapper<T>(sp.GetRequiredService<FluentValidation.IValidator<T>>()))
                .AddScoped(sp => (TValidator)sp.GetRequiredService<FluentValidation.IValidator<T>>());

        /// <summary>
        /// Adds the <typeparamref name="TValidator"/> as a scoped service only.
        /// </summary>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        /// <remarks>Note that this does <i>not</i> register the corresponding <see cref="FluentValidation.IValidator{T}"/> and <see cref="CoreEx.Validation.IValidator{T}"/>; use <see cref="AddFluentValidator{T, TValidator}(IServiceCollection)"/> to explicitly perform.</remarks>
        public static IServiceCollection AddFluentValidator<TValidator>(this IServiceCollection services) where TValidator : class, FluentValidation.IValidator
            => AddFluentValidatorInternal<TValidator>(services);

        /// <summary>
        /// Adds the <see cref="FluentValidation.IValidator"/> as a scoped service only.
        /// </summary>
        private static IServiceCollection AddFluentValidatorInternal<TValidator>(this IServiceCollection services) where TValidator : class, FluentValidation.IValidator
            => (services ?? throw new ArgumentNullException(nameof(services))).AddScoped<TValidator>();

        /// <summary>
        /// Adds all the <see cref="FluentValidation.IValidator{T}"/>(s) and corresponding <see cref="CoreEx.FluentValidation.ValidatorWrapper{T}"/>(s) from the specified <typeparamref name="TAssembly"/> as scoped services.
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="includeInternalTypes">Indicates whether to include internally defined types.</param>
        /// <param name="alsoRegisterInterfaces">Indicates whether to also register the interfaces with <see cref="AddFluentValidator{T, TValidator}(IServiceCollection)"/> (default); otherwise, with <see cref="AddFluentValidator{TValidator}(IServiceCollection)"/> (just the validator instance itself).</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddFluentValidators<TAssembly>(this IServiceCollection services, bool includeInternalTypes = false, bool alsoRegisterInterfaces = true)
        {
            var afv = alsoRegisterInterfaces
                ? typeof(IFluentServiceCollectionExtensions).GetMethod(nameof(AddFluentValidatorWithInterfacesInternal), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)
                : typeof(IFluentServiceCollectionExtensions).GetMethod(nameof(AddFluentValidatorInternal), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);

            foreach (var match in from type in includeInternalTypes ? typeof(TAssembly).Assembly.GetTypes() : typeof(TAssembly).Assembly.GetExportedTypes()
                                  where !type.IsAbstract && !type.IsGenericTypeDefinition
                                  let interfaces = type.GetInterfaces()
                                  let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(FluentValidation.IValidator<>))
                                  let @interface = genericInterfaces.FirstOrDefault()
                                  let valueType = @interface?.GetGenericArguments().FirstOrDefault()
                                  where @interface != null
                                  select new { valueType, type })
            {
                if (alsoRegisterInterfaces)
                    afv.MakeGenericMethod(match.valueType, match.type).Invoke(null, new object[] { services });
                else
                    afv.MakeGenericMethod(match.type).Invoke(null, new object[] { services });
            }

            return services;
        }
    }
}