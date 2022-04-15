// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using CoreEx.Json;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Checks that the <see cref="IServiceCollection"/> is not null.
        /// </summary>
        private static IServiceCollection CheckServices(IServiceCollection services) => services ?? throw new ArgumentNullException(nameof(services));

        /// <summary>
        /// Adds the <see cref="CoreEx.Newtonsoft.Json.JsonSerializer"/> as the <see cref="IJsonSerializer"/> singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddNewtonsoftJsonSerializer(this IServiceCollection services) => CheckServices(services).AddSingleton<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>();

        /// <summary>
        /// Adds the <see cref="System.Text.Json.JsonSerializerOptions"/> as the singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <param name="settings">The <see cref=" Newtonsoft.Json.JsonSerializerSettings"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddNewtonsoftJsonSerializerSettings(this IServiceCollection services, Newtonsoft.Json.JsonSerializerSettings settings) => CheckServices(services).AddSingleton(_ => settings ?? throw new ArgumentNullException(nameof(settings)));

        /// <summary>
        /// Adds the <see cref="CoreEx.Newtonsoft.Json.CloudEventSerializer"/> as the <see cref="IEventSerializer"/> singleton service.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddNewtonsoftCloudEventSerializer(this IServiceCollection services) => CheckServices(services).AddSingleton<IEventSerializer, CoreEx.Newtonsoft.Json.CloudEventSerializer>();

        /// <summary>
        /// Adds the <see cref="CoreEx.Newtonsoft.Json.EventDataSerializer"/> as the <see cref="IEventSerializer"/> singleton service.
        /// </summary>
        /// <param name="configure">The action to enable the <see cref="CoreEx.Newtonsoft.Json.EventDataSerializer"/> to be further configured.</param>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddNewtonsoftEventDataSerializer(this IServiceCollection services, Action<CoreEx.Newtonsoft.Json.EventDataSerializer>? configure = null) => CheckServices(services).AddSingleton<IEventSerializer>(sp =>
        {
            var eds = new CoreEx.Newtonsoft.Json.EventDataSerializer(sp.GetService<IJsonSerializer>(), sp.GetService<EventDataFormatter>());
            configure?.Invoke(eds);
            return eds;
        });
    }
}