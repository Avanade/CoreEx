// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace CoreEx.Caching
{
    /// <summary>
    /// Provides <see cref="IRequestCache"/> extension methods.
    /// </summary>
    public static class RequestCacheExtensions
    {
        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="IEntityKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The cached value where found; otherwise, the default value for the <see cref="Type"/>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        public static bool TryGetValue<T>(this IRequestCache cache, IEntityKey key, out T? value) => cache.TryGetValue((key ?? throw new ArgumentNullException(nameof(key))).EntityKey, out value);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <paramref name="key"/> (converted to a <see cref="CompositeKey"/>).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The cached value where found; otherwise, the default value for the <see cref="Type"/>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        public static bool TryGetValue<T>(this IRequestCache cache, object? key, out T? value) => cache.TryGetValue(new CompositeKey(key), out value);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/> where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the value.</param>
        /// <returns>The cached value (existing or new).</returns>
        public static async Task<T?> GetOrAddAsync<T>(this IRequestCache cache, CompositeKey key, Func<Task<T?>> addFactory)
        {
            if (addFactory == null)
                throw new ArgumentNullException(nameof(addFactory));

            if (cache.TryGetValue<T>(key, out var value))
                return value;

            value = await addFactory().ConfigureAwait(false);
            return cache.SetValue(key, value);
        }

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <paramref name="key"/> (converted to a <see cref="CompositeKey"/>) where it exists; otherwise, adds and returns the value created by the <paramref name="addFactory"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to get or add.</param>
        /// <param name="addFactory">The factory function to create the value.</param>
        /// <returns>The cached value (existing or new).</returns>
        public static Task<T?> GetOrAddAsync<T>(this IRequestCache cache, object? key, Func<Task<T?>> addFactory) => cache.GetOrAddAsync(new CompositeKey(key), addFactory);

        /// <summary>
        /// Sets (adds or overrides) the cache value for the specified <see cref="Type"/> and <paramref name="key"/>1 and returns <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The <paramref name="value"/>.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        public static T? SetValue<T>(this IRequestCache cache, object? key, T? value) => cache.SetValue(new CompositeKey(key), value);

        /// <summary>
        /// Sets (adds or overrides) the cache value for the specified <see cref="IEntityKey"/> <see cref="Type"/> and returns <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The <paramref name="value"/>.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        public static T? SetValue<T>(this IRequestCache cache, T? value) where T : IEntityKey => value is null ? value : cache.SetValue(value.EntityKey, value);

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <see cref="IEntityKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns><c>true</c> where found and removed; otherwise, <c>false</c>.</returns>
        public static bool Remove<T>(this IRequestCache cache, IEntityKey key) => cache.Remove<T>((key ?? throw new ArgumentNullException(nameof(key))).EntityKey);

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <paramref name="key"/> (converted to a <see cref="CompositeKey"/>).
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="cache">The <see cref="IRequestCache"/>.</param>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns><c>true</c> where found and removed; otherwise, <c>false</c>.</returns>
        public static bool Remove<T>(this IRequestCache cache, object? key) => cache.Remove<T>(new CompositeKey(key));
    }
}