// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides common <see cref="Type"/> reflection capabilities.
    /// </summary>
    public class TypeReflector
    {
        /// <summary>
        /// Gets all of the properties (<see cref="PropertyInfo"/>) for a <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to reflect.</param>
        /// <param name="bindingFlags">The <see cref="BindingFlags"/>.</param>
        /// <returns>The corresponding <see cref="PropertyInfo"/> <see cref="Array"/>.</returns>
        /// <remarks>The default <paramref name="bindingFlags"/> where not overridden are: <see cref="BindingFlags.Public"/>, <see cref="BindingFlags.GetProperty"/>, <see cref="BindingFlags.SetProperty"/> and <see cref="BindingFlags.Instance"/>.</remarks>
        public static PropertyInfo[] GetProperties(Type type, BindingFlags? bindingFlags = null)
            => type.ThrowIfNull(nameof(type)).GetProperties(bindingFlags ?? (BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance))
                .Where(x => x.GetIndexParameters().Length == 0).GroupBy(x => x.Name).Select(g => g.First()).ToArray();

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/> for a <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to reflect.</param>
        /// <param name="propertyName">The property name to find.</param>
        /// <param name="bindingFlags">The <see cref="BindingFlags"/>.</param>
        /// <returns>The corresponding <see cref="PropertyInfo"/> where found; otherwise, <c>null</c>.</returns>
        /// <remarks>The default <paramref name="bindingFlags"/> where not overridden are: <see cref="BindingFlags.Public"/>, <see cref="BindingFlags.GetProperty"/>, <see cref="BindingFlags.SetProperty"/> and <see cref="BindingFlags.Instance"/>.</remarks>
        public static PropertyInfo? GetPropertyInfo(Type type, string propertyName, BindingFlags? bindingFlags = null)
        {
            var pis = type.ThrowIfNull(nameof(type)).GetProperties(bindingFlags ?? (BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance))
                .Where(x => x.Name == propertyName.ThrowIfNull(nameof(propertyName))).ToArray();

            return pis.Length switch
            {
                0 => null,
                1 => pis[0],
                _ => pis.FirstOrDefault(x => x.DeclaringType == type) ?? pis.First()
            };
        }

        /// <summary>
        /// Gets (creates) the cached <see cref="TypeReflector{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="TypeReflectorArgs"/>.</param>
        /// <returns>The <see cref="TypeReflector{TEntity}"/>.</returns>
        public static TypeReflector<TEntity> GetReflector<TEntity>(TypeReflectorArgs? args = null)
            => (args ??= TypeReflectorArgs.Default).Cache.GetOrCreate(typeof(TEntity), ce =>
            {
                var tr = new TypeReflector<TEntity>(args);
                args.TypeBuilder?.Invoke(tr);
                return (TypeReflector<TEntity>)ConfigureCacheEntry(ce, tr);
            })!;

        /// <summary>
        /// Gets the <see cref="ITypeReflector"/> for the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="args">The <see cref="TypeReflectorArgs"/>.</param>
        /// <param name="type">The entity <see cref="Type"/>.</param>
        /// <returns>The <see cref="ITypeReflector"/>.</returns>
        public static ITypeReflector GetReflector(TypeReflectorArgs? args, Type type) 
            => (args ??= TypeReflectorArgs.Default).Cache.GetOrCreate(type.ThrowIfNull(nameof(args)), ce =>
            {
                var ec = typeof(TypeReflector<>).MakeGenericType(type).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, [typeof(TypeReflectorArgs)], null)!;
                var tr = (ITypeReflector)ec.Invoke([args]);
                args.TypeBuilder?.Invoke(tr);
                return ConfigureCacheEntry(ce, tr);
            })!;

        /// <summary>
        /// Configure the cache entry setting expirations.
        /// </summary>
        private static ITypeReflector ConfigureCacheEntry(ICacheEntry ce, ITypeReflector tr)
        {
            ce.SetAbsoluteExpiration(tr.Args.AbsoluteExpirationTimespan);
            ce.SetSlidingExpiration(tr.Args.SlidingExpirationTimespan);
            return tr;
        }

        #region Collections

        /// <summary>
        /// Gets the underlying item <see cref="Type"/> where an <see cref="Array"/>, <see cref="IDictionary"/>, <see cref="ICollection"/> or <see cref="IEnumerable"/>. 
        /// </summary>
        /// <param name="type">The <see cref="Type"/>.</param>
        /// <returns>The <see cref="TypeReflectorTypeCode"/> and corresponding item <see cref="Type"/> where a collection.</returns>
        public static (TypeReflectorTypeCode TypeCode, Type? ItemType) GetCollectionItemType(Type type)
        {
            if ((type.ThrowIfNull(nameof(type))) == typeof(string) || type.IsPrimitive || type.IsValueType)
                return (TypeReflectorTypeCode.Simple, null);

            if (type.IsArray)
                return (TypeReflectorTypeCode.Array, type.GetElementType());

            var (_, valueType) = GetDictionaryType(type);
            if (valueType != null)
                return (TypeReflectorTypeCode.IDictionary, valueType);

            var t = GetCollectionType(type);
            if (t != null)
                return (TypeReflectorTypeCode.ICollection, t);

            t = GetEnumerableType(type);
            if (t != null)
                return (TypeReflectorTypeCode.IEnumerable, t);

            var (ItemType, _) = GetEnumerableTypeFromAdd(type);
            if (ItemType != null)
                return (TypeReflectorTypeCode.IEnumerable, ItemType);

            return (TypeReflectorTypeCode.Complex, null);
        }

        /// <summary>
        /// Gets the underlying <see cref="ICollection{T}"/> Type.
        /// </summary>
        private static Type? GetCollectionType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ICollection<>))
                return type.GetGenericArguments()[0];

            var t = type.GetInterfaces().FirstOrDefault(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(ICollection<>));
            if (t == null)
                return null;

            return ((t == typeof(ICollection<>)) ? type : t).GetGenericArguments()[0];
        }

        /// <summary>
        /// Gets the underlying <see cref="IEnumerable"/> Type.
        /// </summary>
        private static Type? GetEnumerableType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                return type.GetGenericArguments()[0];

            var t = type.GetInterfaces().FirstOrDefault(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IEnumerable<>));
            if (t == null)
            {
                t = type.GetInterfaces().FirstOrDefault(x => x == typeof(IEnumerable));
                if (t == null)
                    return null;
            }

            var gas = ((t == typeof(IEnumerable)) ? type : t).GetGenericArguments();
            if (gas.Length == 0)
                return null;

            if (type == typeof(IEnumerable<>).MakeGenericType([gas[0]]))
                return gas[0];

            return null;
        }

        /// <summary>
        /// Gets the underlying <see cref="IDictionary{TKey, TValue}"/> Types.
        /// </summary>
        public static (Type? keyType, Type? valueType) GetDictionaryType(Type type)
        {
            Type? t;
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                t = type;
            else
                t = type.GetInterfaces().FirstOrDefault(x => (x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == typeof(IDictionary<,>)));

            if (t == null)
                return (null, null);

            var gas = t.GetGenericArguments();
            if (gas.Length != 2)
                return (null, null);

            return (gas[0], gas[1]);
        }

        /// <summary>
        /// Gets the underlying <see cref="IEnumerable"/> Type by inferring from the Add method.
        /// </summary>
        private static (Type? ItemType, MethodInfo? AddMethod) GetEnumerableTypeFromAdd(Type type)
        {
            var mi = type.GetMethod("Add");
            if (mi == null)
                return (null, null);

            var ps = mi.GetParameters();
            return ps.Length == 1 ? (ps[0].ParameterType, mi) : (null, null);
        }

        #endregion
    }
}