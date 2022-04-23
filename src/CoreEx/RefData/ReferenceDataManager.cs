// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.RefData
{
    /// <summary>
    /// 
    /// </summary>
    public class ReferenceDataManager
    {
        private readonly ConcurrentDictionary<Type, IReferenceDataProvider> _typeToProvider = new();

        /// <summary>
        /// Gets the current <see cref="ReferenceDataManager"/> from the <see cref="IServiceProvider"/> within the <see cref="ExecutionContext.Current"/> <see cref="ExecutionContext"/> scope (see <see cref="ExecutionContext.GetService(Type)"/>).
        /// </summary>
        public static ReferenceDataManager Current => 
            ExecutionContext.GetService<ReferenceDataManager>() ?? throw new InvalidOperationException($"To access {nameof(ReferenceDataManager)}.{nameof(Current)} it must be added as a Dependency Injection service ({nameof(IServiceProvider)}) and the request must be mande within an {nameof(ExecutionContext)} scope.");

        /// <summary>
        /// Registers one or more <see cref="IReferenceDataProvider"/> provider instances.
        /// </summary>
        /// <param name="providers">The <see cref="IReferenceDataProvider"/> provider instances.</param>
        /// <remarks>Internally this builds the relationship between the <see cref="IReferenceDataProvider.Types"/> and the owning <see cref="IReferenceDataProvider"/> to enable cached access to the underlying <see cref="IReferenceDataCollection"/> using <see cref="GetByType(Type)"/> or <see cref="this[Type]"/>.</remarks>
        public void Register(params IReferenceDataProvider[] providers)
        {
            if (providers == null || providers.Length == 0)
                return;

            foreach (var provider in providers.Where(x => x != null).Distinct())
            {
                foreach (var type in provider.Types.Where(x => x != null).Distinct())
                {
                    if (!_typeToProvider.TryAdd(type, provider))
                        throw new InvalidOperationException($"Type '{type.FullName}' cannot be added as already associated with previously added Provider '{_typeToProvider.GetValueOrDefault(type)?.GetType().FullName}'.");
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? this[Type type] => GetByType(type);

        /// <summary>
        /// Gets the <see cref="IReferenceDataCollection"/> for the specified <see cref="IReferenceData"/> <see cref="Type"/>. 
        /// </summary>
        /// <param name="type">The <see cref="IReferenceData"/> <see cref="Type"/>.</param>
        /// <returns>The corresponding <see cref="IReferenceDataCollection"/> where found; otherwise, <c>null</c>.</returns>
        public IReferenceDataCollection? GetByType(Type type) => _typeToProvider.TryGetValue(type ?? throw new ArgumentNullException(nameof(type)), out var provider) ? provider[type] : null;
    }
}