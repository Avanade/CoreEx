// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using CoreEx.Entities;
using System;
using System.Diagnostics.CodeAnalysis;

namespace CoreEx.Caching
{
    /// <summary>
    /// Enables the short-lived request caching; intended to reduce data chattiness within the context of a request scope.
    /// </summary>
    public interface IRequestCache
    {
        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The cached value where found; otherwise, the default value for the <see cref="Type"/>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        bool TryGetValue<T>(CompositeKey key, out T? value);

        /// <summary>
        /// Sets (adds or overrides) the cache value for the specified <see cref="Type"/> and <see cref="CompositeKey"/> and returns <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The <paramref name="value"/>.</returns>
        [return: NotNullIfNotNull(nameof(value))]
        T? SetValue<T>(CompositeKey key, T? value);

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns><c>true</c> where found and removed; otherwise, <c>false</c>.</returns>
        bool Remove<T>(CompositeKey key);

        /// <summary>
        /// Clears the cache for the specified <see cref="Type"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        void Clear<T>();

        /// <summary>
        /// Clears the cache for all <see cref="Type">types</see>.
        /// </summary>
        void ClearAll();
    }
}