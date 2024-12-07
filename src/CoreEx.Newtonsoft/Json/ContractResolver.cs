// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using CoreEx.Entities.Extended;
using CoreEx.Events;
using CoreEx.RefData;
using CoreEx.RefData.Extended;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using Stj = System.Text.Json.Serialization;

namespace CoreEx.Newtonsoft.Json
{
    /// <summary>
    /// Extends the <see cref="CamelCasePropertyNamesContractResolver"/> to enable runtime configurable of JSON serialization property rename and ignore.
    /// </summary>
    public class ContractResolver : CamelCasePropertyNamesContractResolver
    {
        private readonly static ContractResolver _default = new();

        private HashSet<Type>? _typeDict;
        private ConcurrentDictionary<Type, Dictionary<string, string>>? _renameDict;
        private ConcurrentDictionary<Type, HashSet<string>>? _ignoreDict;

        /// <summary>
        /// Static constructor.
        /// </summary>
        static ContractResolver()
        {
            _default.AddType<EntityCore>()
                    .AddType<EntityBase>()
                    .AddType(typeof(ReferenceDataBaseEx<,>))
                    .AddType(typeof(ReferenceDataBase<>))
                    .AddType<MessageItem>()
                    .AddType<PagingArgs>()
                    .AddType<PagingResult>()
                    .AddType<EventDataBase>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractResolver"/>.
        /// </summary>
        public ContractResolver() => NamingStrategy = SubstituteNamingStrategy.Substitute;

        /// <summary>
        /// Gets the default <see cref="ContractResolver"/>.
        /// </summary>
        /// <remarks>Automatically adds the serialization property renames and ignores for <see cref="EventDataBase"/>, <see cref="EventData"/>, <see cref="EventData{T}"/>, <see cref="ChangeLog"/> and <see cref="ReferenceDataBase{T}"/> types.</remarks>
        public static ContractResolver Default => _default;

        /// <summary>
        /// Adds the <typeparamref name="T"/> <see cref="Type"/> by reflecting all <c>System.Text.Json.Serialization</c> property attributes to infer rename (<see cref="Stj.JsonPropertyNameAttribute"/>) or ignore (<see cref="Stj.JsonIgnoreAttribute"/>).
        /// </summary>
        /// <typeparam name="T">The <see cref="Type"/>.</typeparam>
        /// <returns>The <see cref="ContractResolver"/> instance to support fluent-style method-chaining.</returns>
        public ContractResolver AddType<T>() => AddType(typeof(T));

        /// <summary>
        /// Adds the <paramref name="type"/> by reflecting all <c>System.Text.Json.Serialization</c> property attributes to infer rename (<see cref="Stj.JsonPropertyNameAttribute"/>) or ignore (<see cref="Stj.JsonIgnoreAttribute"/>).
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="ContractResolver"/> instance to support fluent-style method-chaining.</returns>
        public ContractResolver AddType(Type type)
        {
            (_typeDict ??= []).Add(type);
            return this;
        }

        /// <summary>
        /// Renames one or more properties for a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="pairs">One or more property and JSON name pairs.</param>
        /// <returns>The <see cref="ContractResolver"/> instance to support fluent-style method-chaining.</returns>
        public ContractResolver AddRename(Type type, IDictionary<string, string> pairs)
        {
            type.ThrowIfNull(nameof(type));

            if (pairs != null)
            {
                foreach (var pair in pairs)
                {
                    AddRename(type, pair.Key, pair.Value);
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
        public ContractResolver AddRename(Type type, string propertyName, string jsonName)
        {
            type.ThrowIfNull(nameof(type));
            propertyName.ThrowIfNullOrEmpty(nameof(propertyName));
            jsonName.ThrowIfNullOrEmpty(nameof(jsonName));

            if (propertyName == jsonName)
                return this;

            (_renameDict ??= new ConcurrentDictionary<Type, Dictionary<string, string>>()).AddOrUpdate(type, t => new Dictionary<string, string> { { propertyName, jsonName } }, (t, d) => { d.TryAdd(propertyName, jsonName); return d; });

            return this;
        }

        /// <summary>
        /// Ignores one or more properties for a <see cref="Type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <param name="propertyNames">One or more property names.</param>
        /// <returns>The <see cref="ContractResolver"/> instance to support fluent-style method-chaining.</returns>
        public ContractResolver AddIgnore(Type type, params string[] propertyNames)
        {
            type.ThrowIfNull(nameof(type));

            if (propertyNames == null || propertyNames.Length == 0)
                return this;

            (_ignoreDict ??= new ConcurrentDictionary<Type, HashSet<string>>()).AddOrUpdate(type, t => new HashSet<string>(propertyNames), (t, hs) => { hs.UnionWith(propertyNames); return hs; });
            return this;
        }

        /// <inheritdoc/>
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);
            var type = property.DeclaringType!.IsGenericType ? property.DeclaringType!.GetGenericTypeDefinition() : property.DeclaringType!;

            if (_ignoreDict != null && _ignoreDict.TryGetValue(type, out var hs))
            {
                if (hs.Contains(property.PropertyName!))
                {
                    property.ShouldSerialize = x => false;
                    property.Ignored = true;
                    return property;
                }
            }

            if (_renameDict != null && _renameDict.TryGetValue(type, out var d) && d.TryGetValue(property.PropertyName!, out var jn))
            {
                property.PropertyName = jn;
                return property;
            }
            
            if (_typeDict != null && _typeDict.TryGetValue(type, out _))
            {
                var jpna = member.GetCustomAttribute<Stj.JsonPropertyNameAttribute>(true);
                if (jpna != null)
                {
                    property.PropertyName = jpna.Name;
                    property.Order = member.GetCustomAttribute<Stj.JsonPropertyOrderAttribute>(true)?.Order;
                    return property;
                }

                var jpia = member.GetCustomAttribute<Stj.JsonIgnoreAttribute>(true);
                if (jpia != null)
                {
                    property.ShouldSerialize = x => false;
                    property.Ignored = true;
                }
            }

            return property;
        }

        /// <summary>
        /// Gets (creates) the <see cref="JsonProperty"/> from the <paramref name="memberInfo"/>.
        /// </summary>
        /// <param name="memberInfo">The <see cref="MemberInfo"/>.</param>
        /// <param name="memberSerialization">The <see cref="MemberSerialization"/> option.</param>
        /// <returns>The <see cref="JsonProperty"/>.</returns>
        public JsonProperty GetProperty(MemberInfo memberInfo, MemberSerialization memberSerialization) => CreateProperty(memberInfo, memberSerialization);
    }
}