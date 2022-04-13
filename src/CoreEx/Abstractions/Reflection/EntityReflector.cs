// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace CoreEx.Abstractions.Reflection
{
    /// <summary>
    /// Provides common entity (class) <see cref="Type"/> reflection capabilities.
    /// </summary>
    public class EntityReflector
    {
        /// <summary>
        /// Gets all of the properties (<see cref="PropertyInfo"/>) for a <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to reflect.</param>
        /// <returns>The corresponding <see cref="PropertyInfo"/> <see cref="Array"/>.</returns>
        public static PropertyInfo[] GetProperties(Type type)
            => (type ?? throw new ArgumentNullException(nameof(type))).GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite).GroupBy(x => x.Name).Select(g => g.First()).ToArray();

        /// <summary>
        /// Gets the <see cref="PropertyInfo"/> for a <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> to reflect.</param>
        /// <param name="propertyName">The property name to find.</param>
        /// <returns>The corresponding <see cref="PropertyInfo"/> where found; otherwise, <c>null</c>.</returns>
        public static PropertyInfo? GetPropertyInfo(Type type, string propertyName)
        {
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var pis = (type ?? throw new ArgumentNullException(nameof(type))).GetProperties(BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance)
                .Where(x => x.Name == propertyName && x.CanRead && x.CanWrite).ToArray();

            return pis.Length switch
            {
                0 => null,
                1 => pis[0],
                _ => pis.FirstOrDefault(x => x.DeclaringType == type) ?? pis.First()
            };
        }

        /// <summary>
        /// Gets (creates) the cached <see cref="EntityReflector{TEntity}"/>.
        /// </summary>
        /// <typeparam name="TEntity">The entity <see cref="Type"/>.</typeparam>
        /// <param name="args">The <see cref="EntityReflectorArgs"/>.</param>
        /// <returns>The <see cref="EntityReflector{TEntity}"/>.</returns>
        public static EntityReflector<TEntity> GetReflector<TEntity>(EntityReflectorArgs args) where TEntity : class, new() 
            => (EntityReflector<TEntity>)(args ?? throw new ArgumentNullException(nameof(args))).Cache.GetOrAdd(typeof(TEntity), (type) =>
            {
                var er = new EntityReflector<TEntity>(args);
                args.EntityBuilder?.Invoke(er);
                return er;
            });

        /// <summary>
        /// Gets the <see cref="IEntityReflector"/> for the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="args">The <see cref="EntityReflectorArgs"/>.</param>
        /// <param name="type">The entity <see cref="Type"/>.</param>
        /// <returns>The <see cref="IEntityReflector"/>.</returns>
        public static IEntityReflector GetReflector(EntityReflectorArgs args, Type type) 
            => (args ?? throw new ArgumentNullException(nameof(args))).Cache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(args)), _ =>
            {
                if (!type.IsClass || type == typeof(string) || type.GetConstructor(Type.EmptyTypes) == null)
                    throw new ArgumentException($"Type '{type.Name}' must be a class and have a default (parameter-less) constructor.", nameof(type));

                var ec = typeof(EntityReflector<>).MakeGenericType(type).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(EntityReflectorArgs) }, null);
                var er = (IEntityReflector)ec.Invoke(new object[] { args });
                args.EntityBuilder?.Invoke(er);
                return er;
            });
    }
}