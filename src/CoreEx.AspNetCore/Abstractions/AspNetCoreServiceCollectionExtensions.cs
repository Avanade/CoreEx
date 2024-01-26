// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.AspNetCore.WebApis;
using CoreEx.Configuration;
using CoreEx.Events;
using CoreEx.Json;
using CoreEx.Json.Merge;
using Microsoft.Extensions.Logging;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class AspNetCoreServiceCollectionExtensions
    {
        /// <summary>
        /// Checks that the <see cref="IServiceCollection"/> is not null.
        /// </summary>
        private static IServiceCollection CheckServices(IServiceCollection services) => services.ThrowIfNull(nameof(services));

        /// <summary>
        /// Adds the <see cref="WebApi"/> as a scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="WebApi"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddWebApi(this IServiceCollection services, Action<WebApi>? configure = null) => CheckServices(services).AddScoped(sp =>
        {
            var wa = new WebApi(sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<IJsonSerializer>(), sp.GetRequiredService<ILogger<WebApi>>(), sp.GetService<WebApiInvoker>(), sp.GetService<IJsonMergePatch>());
            configure?.Invoke(wa);
            return wa;
        });

        /// <summary>
        /// Adds the <see cref="ReferenceDataContentWebApi"/> as a scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="WebApi"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddReferenceDataContentWebApi(this IServiceCollection services, Action<ReferenceDataContentWebApi>? configure = null) => CheckServices(services).AddScoped(sp =>
        {
            var wa = new ReferenceDataContentWebApi(sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<IReferenceDataContentJsonSerializer>(), sp.GetRequiredService<ILogger<WebApi>>(), sp.GetService<WebApiInvoker>(), sp.GetService<IJsonMergePatch>());
            configure?.Invoke(wa);
            return wa;
        });

        /// <summary>
        /// Adds the <see cref="WebApiPublisher"/> as a scoped service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="configure">The action to enable the <see cref="WebApi"/> to be further configured.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddWebApiPublisher(this IServiceCollection services, Action<WebApiPublisher>? configure = null) => CheckServices(services).AddScoped(sp =>
        {
            var wap = new WebApiPublisher(sp.GetRequiredService<IEventPublisher>(), sp.GetRequiredService<ExecutionContext>(), sp.GetRequiredService<SettingsBase>(), sp.GetRequiredService<IJsonSerializer>(), sp.GetRequiredService<ILogger<WebApiPublisher>>(), sp.GetService<WebApiInvoker>());
            configure?.Invoke(wap);
            return wap;
        });
    }
}