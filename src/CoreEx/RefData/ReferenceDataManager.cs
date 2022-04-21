// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;

namespace CoreEx.RefData
{
    /// <summary>
    /// Provides a standard mechanism for managing and accessing all the available/possible <see cref="IReferenceData"/> types via the <see cref="Current"/> property. 
    /// </summary>
    /// <remarks>
    /// This is required as the likes of model/entity classes where they contain no implementation specific logic, such as database access, etc. This enables the <b>ReferenceData</b> access logic to be seperated from
    /// the implementation. This ensures that the <b>ReferenceData</b> follows the same consistent pattern/layering for the implementation logic.
    /// <para>The <see cref="Register"/> (secondary) enables overriding of the <see cref="IReferenceDataProvider"/> where the default <see cref="ExecutionContext.ServiceProvider"/> (i.e. dependency injection) is not configured (primary).</para>
    /// </remarks>
    public sealed class ReferenceDataManager
    {
        private static readonly ConcurrentDictionary<Type, IReferenceDataProvider> _providers = new();
        private static readonly ConcurrentDictionary<Type, Type> _refTypeToInterface = new();

        /// <summary>
        /// Gets the current <see cref="ReferenceDataManager"/> instance. 
        /// </summary>
        public static ReferenceDataManager Current { get; } = new ReferenceDataManager();

        /// <summary>
        /// Registers one or more <see cref="IReferenceDataProvider"/> provider instances.
        /// </summary>
        /// <param name="providers">The <see cref="IReferenceDataProvider"/> provider instances.</param>
        public static void Register(params IReferenceDataProvider[] providers)
        {
            if (providers == null || providers.Length == 0)
                return;

            foreach (var provider in providers.Where(p => p != null))
            {
                if (!_providers.TryAdd(provider.ProviderType, provider))
                    throw new ArgumentException($"Provider Type '{provider.ProviderType.FullName}' has already been registered.");
            }
        }

        /// <summary>
        /// Gets the <paramref name="providerType"/> <see cref="IReferenceDataProvider"/> instance using dependency injection (see <see cref="ExecutionContext.GetService(Type)"/>) or from those 
        /// <see cref="Register(IReferenceDataProvider[])">pre-registered</see>.
        /// </summary>
        /// <param name="providerType">The <see cref="IReferenceDataProvider.ProviderType"/>.</param>
        /// <returns>The <see cref="IReferenceDataProvider"/> instance.</returns>
        public static IReferenceDataProvider GetProvider(Type providerType)
        {
            var service = ExecutionContext.GetService(providerType ?? throw new ArgumentNullException(nameof(providerType)));
            if (service != null)
                return (IReferenceDataProvider)service;

            if (_providers.TryGetValue(providerType, out var provider))
                return provider;

            throw new ArgumentException($"Provider '{providerType.FullName}' has not been registered. Either register using dependency injection (primary) or using the Register method (secondary). " +
                "For dependency injection this indicates that the ExecutionContext.ServiceProvider property has not been set.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceDataManager"/>.
        /// </summary>
        private ReferenceDataManager() { }

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/>.</returns>
        public IReferenceDataCollection this[Type type]
        {
            get
            {
                var interfaceType = _refTypeToInterface.GetOrAdd(type ?? throw new ArgumentNullException(nameof(type)), rdt =>
                {
                    var rdi = rdt.GetCustomAttribute<ReferenceDataInterfaceAttribute>();
                    if (rdi == null || !rdi.InterfaceType.IsInterface)
                        throw new ArgumentException($"Type '{rdt.Name}' must have the {nameof(ReferenceDataInterfaceAttribute)} assigned and the corresponding {nameof(ReferenceDataInterfaceAttribute.InterfaceType)} value must be an interface.");

                    return rdi.InterfaceType;
                });

                return GetProvider(interfaceType)[type];
            }
        }

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/>.</returns>
        public IReferenceDataCollection GetByType(Type type) => this[type];
    }
}