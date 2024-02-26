// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx;
using CoreEx.Events;
using CoreEx.Json;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Provides <see cref="IServiceCollection"/> extensions.
    /// </summary>
    public static class NewtonsoftServiceCollectionExtensions
    {
        /// <summary>
        /// Checks that the <see cref="IServiceCollection"/> is not null.
        /// </summary>
        private static IServiceCollection CheckServices(IServiceCollection services) => services.ThrowIfNull(nameof(services));

        /// <summary>
        /// Adds the <see cref="CoreEx.Newtonsoft.Json.JsonSerializer"/> as the <see cref="IJsonSerializer"/> and <see cref="CoreEx.Newtonsoft.Json.ReferenceDataContentJsonSerializer"/> as the <see cref="IReferenceDataContentJsonSerializer"/> singleton services.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/>.</param>
        /// <returns>The <see cref="IServiceCollection"/>.</returns>
        public static IServiceCollection AddNewtonsoftJsonSerializer(this IServiceCollection services) 
            => CheckServices(services).AddSingleton<IJsonSerializer, CoreEx.Newtonsoft.Json.JsonSerializer>()
                                      .AddSingleton<IReferenceDataContentJsonSerializer, CoreEx.Newtonsoft.Json.ReferenceDataContentJsonSerializer>();

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