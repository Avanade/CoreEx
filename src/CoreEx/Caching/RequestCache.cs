// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace CoreEx.Caching
{
    /// <summary>
    /// Provides a basic concurrent dictionary backed cache for short-lived data within the context of a request scope to reduce data-layer chattiness.
    /// </summary>
    public class RequestCache : IRequestCache
    {
        private readonly Lazy<ConcurrentDictionary<(Type, CompositeKey), object?>> _caching = new(true);

        /// <inheritdoc/>
        public bool TryGetValue<T>(CompositeKey key, out T? value)
        {
            if (_caching.IsValueCreated && _caching.Value.TryGetValue(new (typeof(T), key), out object? val))
            {
                value = (T?)val;
                return true;
            }

            value = default!;
            return false;
        }

        /// <inheritdoc/>
        [return: NotNullIfNotNull(nameof(value))]
        public T? SetValue<T>(CompositeKey key, T? value)
        {
            _caching.Value.AddOrUpdate(new(typeof(T), key), value, (_, __) => value);
            return value;
        }

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and key.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns><c>true</c> where found and removed; otherwise, <c>false</c>.</returns>
        public bool Remove<T>(CompositeKey key) => _caching.IsValueCreated && _caching.Value.TryRemove(new (typeof(T), key), out _);

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
                _caching.Value.TryRemove(item.Key, out _);
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