// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Events;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// 
    /// </summary>
    public class ContractResolver : DefaultContractResolver
    {
        private readonly static ContractResolver _default = new();
        private readonly ConcurrentDictionary<Type, Dictionary<string, string>> _renameDict = new();
        private readonly ConcurrentDictionary<Type, HashSet<string>> _ignoreDict = new();

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ContractResolver()
        {
            _default.Rename(typeof(EventDataBase), new Dictionary<string, string> 
            { 
                { nameof(EventDataBase.Id), "id" },
                { nameof(EventDataBase.Subject), "subject" },
                { nameof(EventDataBase.Action), "action" },
                { nameof(EventDataBase.Type), "type" },
                { nameof(EventDataBase.Source), "source" },
                { nameof(EventDataBase.Timestamp), "timestamp" },
                { nameof(EventDataBase.CorrelationId), "correlationId" },
                { nameof(EventDataBase.TenantId), "tenantId" },
                { nameof(EventDataBase.PartitionKey), "partitionKey" },
                { nameof(EventDataBase.ETag), "etag" },
                { nameof(EventDataBase.Attributes), "attributes" },
            });

            _default.Rename(typeof(EventData), nameof(EventData.Value), "value");
            _default.Rename(typeof(EventData<>), nameof(EventData.Value), "value");
        }

        /// <summary>
        /// Gets the default <see cref="ContractResolver"/>.
        /// </summary>
        /// <remarks>Automatically adds the serialization property renames and ignores for <see cref="EventDataBase"/>, <see cref="EventData"/> and <see cref="EventData{T}"/> types.</remarks>
        public static ContractResolver Default => _default;

        /// <summary>
        /// Renames one or more properties for a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="pairs">One or more property and JSON name pairs.</param>
        /// <returns>The <see cref="ContractResolver"/> instance to support fluent-style method-chaining.</returns>
        public ContractResolver Rename(Type type, IDictionary<string, string> pairs)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (pairs != null)
            {
                foreach (var pair in pairs)
                {
                    Rename(type, pair.Key, pair.Value);
                }
            }

            return this;
        }

        /// <summary>
        /// Renames the <paramref name="type"/> property to the <paramref name="jsonName"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="propertyName">The property name.</param>
        /// <param name="jsonName">The JSON name.</param>
        /// <returns>The <see cref="ContractResolver"/> instance to support fluent-style method-chaining.</returns>
        public ContractResolver Rename(Type type, string propertyName, string jsonName)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (string.IsNullOrEmpty(propertyName))
                throw new ArgumentNullException(nameof(propertyName));

            if (string.IsNullOrEmpty(jsonName))
                throw new ArgumentNullException(nameof(jsonName));

            if (propertyName == jsonName)
                return this;

            _renameDict.AddOrUpdate(type, t => new Dictionary<string, string> { { propertyName, jsonName } }, (t, d) => { d.TryAdd(propertyName, jsonName); return d; });

            return this;
        }

        /// <summary>
        /// Ignores one or more properties for a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="propertyNames">One or more property names.</param>
        /// <returns>The <see cref="ContractResolver"/> instance to support fluent-style method-chaining.</returns>
        public ContractResolver Ignore(Type type, params string[] propertyNames)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));

            if (propertyNames == null || propertyNames.Length == 0)
                return this;

            _ignoreDict.AddOrUpdate(type, t => new HashSet<string>(propertyNames), (t, hs) => { hs.UnionWith(propertyNames); return hs; });
            return this;
        }

        /// <inheritdoc/>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var type = property.DeclaringType!.IsGenericType ? property.DeclaringType!.GetGenericTypeDefinition() : property.DeclaringType!;

            if (_ignoreDict.TryGetValue(type, out var hs))
            {
                if (hs.Contains(property.PropertyName!))
                {
                    property.ShouldSerialize = i => false;
                    property.Ignored = true;
                    return property;
                }
            }

            if (_renameDict.TryGetValue(type, out var d) && d.TryGetValue(property.PropertyName!, out var jn))
                property.PropertyName = jn;

            return property;
        }
    }
}