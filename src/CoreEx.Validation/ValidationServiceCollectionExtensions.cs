// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Localization;
using CoreEx.Validation;
using System;
using System.Linq;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ValidationServiceCollectionExtensions
    {
        /// <summary>
        /// Adds the <see cref="IValidatorEx{T}"/>, the <see cref="IValidator{T}"/>, and <typeparamref name="TValidator"/> as scoped services.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidator<T, TValidator>(this IServiceCollection services) where TValidator : class, IValidatorEx<T>
            => AddValidatorWithInterfacesInternal<T, TValidator>(services);

        /// <summary>
        /// Adds the <see cref="IValidatorEx{T}"/>, the <see cref="IValidator{T}"/>, and <typeparamref name="TValidator"/> as scoped services.
        /// </summary>
        private static IServiceCollection AddValidatorWithInterfacesInternal<T, TValidator>(this IServiceCollection services) where TValidator : class, IValidatorEx<T>
            => services.ThrowIfNull(nameof(services))
                .AddScoped<IValidatorEx<T>, TValidator>()
                .AddScoped<IValidator<T>>(sp => sp.GetRequiredService<IValidatorEx<T>>())
                .AddScoped(sp => (TValidator)sp.GetRequiredService<IValidatorEx<T>>());

        /// <summary>
        /// Adds the <typeparamref name="TValidator"/> as a scoped service only.
        /// </summary>
        /// <typeparam name="TValidator">The validator <see cref="Type"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        /// <remarks>Note that this does <i>not</i> register the corresponding <see cref="IValidatorEx{T}"/> and <see cref="IValidator{T}"/>; use <see cref="AddValidator{T, TValidator}(IServiceCollection)"/> to explicitly perform.</remarks>
        public static IServiceCollection AddValidator<TValidator>(this IServiceCollection services) where TValidator : class, IValidatorEx
            => AddValidatorInternal<TValidator>(services);

        /// <summary>
        /// Adds the <typeparamref name="TValidator"/> as a scoped service only.
        /// </summary>
        private static IServiceCollection AddValidatorInternal<TValidator>(this IServiceCollection services) where TValidator : class, IValidatorEx
            => services.ThrowIfNull(nameof(services)).AddScoped<TValidator>();

        /// <summary>
        /// Adds all the <see cref="IValidatorEx{T}"/> validators from the specified <typeparamref name="TAssembly"/> as scoped services.
        /// </summary>
        /// <typeparam name="TAssembly">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidators<TAssembly>(this IServiceCollection services)
            => AddValidators(services, [typeof(TAssembly).Assembly]);

        /// <summary>
        /// Adds all the <see cref="IValidatorEx{T}"/> validators from the specified <typeparamref name="TAssembly1"/> and <typeparamref name="TAssembly2"/> as scoped services.
        /// </summary>
        /// <typeparam name="TAssembly1">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <typeparam name="TAssembly2">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidators<TAssembly1, TAssembly2>(this IServiceCollection services)
            => AddValidators(services, [typeof(TAssembly1).Assembly, typeof(TAssembly2).Assembly]);

        /// <summary>
        /// Adds all the <see cref="IValidatorEx{T}"/> validators from the specified <typeparamref name="TAssembly1"/>, <typeparamref name="TAssembly2"/> and <typeparamref name="TAssembly3"/> as scoped services.
        /// </summary>
        /// <typeparam name="TAssembly1">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <typeparam name="TAssembly2">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <typeparam name="TAssembly3">The <see cref="Type"/> to infer the underlying <see cref="Type.Assembly"/>.</typeparam>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidators<TAssembly1, TAssembly2, TAssembly3>(this IServiceCollection services)
            => AddValidators(services, [typeof(TAssembly1).Assembly, typeof(TAssembly2).Assembly, typeof(TAssembly3).Assembly]);

        /// <summary>
        /// Adds all the <see cref="IValidatorEx{T}"/> validators from the specified <paramref name="assemblies"/> as scoped services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="assemblies">The assemblies.</param>
        /// <param name="includeInternalTypes">Indicates whether to include internally defined types.</param>
        /// <param name="alsoRegisterInterfaces">Indicates whether to also register the interfaces with <see cref="AddValidator{T, TValidator}(IServiceCollection)"/> (default); otherwise, with <see cref="AddValidator{TValidator}(IServiceCollection)"/> (just the validator instance itself).</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidators(this IServiceCollection services, Assembly[] assemblies, bool includeInternalTypes = false, bool alsoRegisterInterfaces = true)
        {
            services.ThrowIfNull(nameof(services));

            foreach (var assembly in assemblies.Distinct())
            {
                var av = alsoRegisterInterfaces
                    ? typeof(ValidationServiceCollectionExtensions).GetMethod(nameof(AddValidatorWithInterfacesInternal), BindingFlags.Static | BindingFlags.NonPublic)!
                    : typeof(ValidationServiceCollectionExtensions).GetMethod(nameof(AddValidatorInternal), BindingFlags.Static | BindingFlags.NonPublic)!;

                foreach (var match in from type in includeInternalTypes ? assembly.GetTypes() : assembly.GetExportedTypes()
                                      where !type.IsAbstract && !type.IsGenericTypeDefinition
                                      let interfaces = type.GetInterfaces()
                                      let genericInterfaces = interfaces.Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidatorEx<>))
                                      let @interface = genericInterfaces.FirstOrDefault()
                                      let valueType = @interface?.GetGenericArguments().FirstOrDefault()
                                      where @interface != null
                                      select new { valueType, type })
                {
                    if (alsoRegisterInterfaces)
                        av.MakeGenericMethod(match.valueType, match.type).Invoke(null, [services]);
                    else
                        av.MakeGenericMethod(match.type).Invoke(null, [services]);
                }
            }

            return services;
        }

        /// <summary>
        /// Adds the <see cref="ValidationTextProvider"/> as the <see cref="ITextProvider"/> as a singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/> for fluent-style method-chaining.</returns>
        public static IServiceCollection AddValidationTextProvider(this IServiceCollection services)
            => services.ThrowIfNull(nameof(services)).AddSingleton<ITextProvider, ValidationTextProvider>();
    }
}