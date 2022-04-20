// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
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
            => (type ?? throw new ArgumentNullException(nameof(type))).GetProperties(bindingFlags ?? BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance)
                .Where(x => x.CanRead && x.CanWrite && x.GetIndexParameters().Length == 0).GroupBy(x => x.Name).Select(g => g.First()).ToArray();

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
            if (propertyName == null)
                throw new ArgumentNullException(nameof(propertyName));

            var pis = (type ?? throw new ArgumentNullException(nameof(type))).GetProperties(bindingFlags ?? BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Instance)
                .Where(x => x.Name == propertyName && x.CanRead && x.CanWrite).ToArray();

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
        public static TypeReflector<TEntity> GetReflector<TEntity>(TypeReflectorArgs args)
            => (TypeReflector<TEntity>)(args ?? throw new ArgumentNullException(nameof(args))).Cache.GetOrAdd(typeof(TEntity), (type) =>
            {
                var er = new TypeReflector<TEntity>(args);
                args.TypeBuilder?.Invoke(er);
                return er;
            });

        /// <summary>
        /// Gets the <see cref="ITypeReflector"/> for the specified <paramref name="type"/>.
        /// </summary>
        /// <param name="args">The <see cref="TypeReflectorArgs"/>.</param>
        /// <param name="type">The entity <see cref="Type"/>.</param>
        /// <returns>The <see cref="ITypeReflector"/>.</returns>
        public static ITypeReflector GetReflector(TypeReflectorArgs args, Type type) 
            => (args ?? throw new ArgumentNullException(nameof(args))).Cache.GetOrAdd(type ?? throw new ArgumentNullException(nameof(args)), _ =>
            {
                var ec = typeof(TypeReflector<>).MakeGenericType(type).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new Type[] { typeof(TypeReflectorArgs) }, null);
                var er = (ITypeReflector)ec.Invoke(new object[] { args });
                args.TypeBuilder?.Invoke(er);
                return er;
            });
    }
}