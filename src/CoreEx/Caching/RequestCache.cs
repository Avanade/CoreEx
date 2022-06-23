// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CoreEx.Caching
{
    /// <summary>
    /// Provides a basic dictionary backed cache for short-lived data within the context of a request scope to reduce data chattiness.
    /// </summary>
    public class RequestCache : IRequestCache
    {
        private readonly Lazy<ConcurrentDictionary<(Type, CompositeKey), object>> _caching = new(true);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and key.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The cached value where found; otherwise, the default value for the <see cref="Type"/>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(CompositeKey key, [NotNullWhen(true)] out T? value)
        {
            if (_caching.IsValueCreated && _caching.Value.TryGetValue(new (typeof(T), key), out object val))
            {
                value = (T)val;
                return true;
            }

            value = default!;
            return false;
        }

        /// <summary>
        /// Sets (adds or overrides) the cache value for the specified <see cref="Type"/> and key.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        public void SetValue<T>(CompositeKey key, T value) => _caching.Value.AddOrUpdate(new (typeof(T), key), value!, (x, y) => value!);

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and key.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns><c>true</c> where found and removed; otherwise, <c>false</c>.</returns>
        public bool Remove<T>(CompositeKey key) => (_caching.IsValueCreated) ? _caching.Value.TryRemove(new (typeof(T), key), out object _) : false;

        /// <summary>
        /// Clears the cache for the specified <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        public void Clear<T>()
        {
            if (!_caching.IsValueCreated)
                return;

            foreach (var item in _caching.Value.Where(x => x.Key.Item1 == typeof(T)).ToList())
            {
                _caching.Value.TryRemove(item.Key, out object val);
            }
        }

        /// <summary>
        /// Clears the cache for all <see cref="Type">types</see>.
        /// </summary>
        public void ClearAll()
        {
            if (_caching.IsValueCreated)
                _caching.Value.Clear();
        }
    }
}