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
        bool TryGetValue<T>(CompositeKey key, [NotNullWhen(true)] out T? value);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="IPrimaryKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to get.</param>
        /// <param name="value">The cached value where found; otherwise, the default value for the <see cref="Type"/>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(IPrimaryKey key, [NotNullWhen(true)] out T? value) => TryGetValue((key ?? throw new ArgumentNullException(nameof(key))).PrimaryKey, out value);

        /// <summary>
        /// Gets the cached value associated with the specified <see cref="Type"/> and <see cref="IPrimaryKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="identifier">The identifier of the value to get.</param>
        /// <param name="value">The cached value where found; otherwise, the default value for the <see cref="Type"/>.</param>
        /// <returns><c>true</c> where found; otherwise, <c>false</c>.</returns>
        public bool TryGetValue<T>(IIdentifier identifier, [NotNullWhen(true)] out T? value) => TryGetValue(new CompositeKey(identifier.Id), out value);

        /// <summary>
        /// Sets (adds or overrides) the cache value for the specified <see cref="Type"/> and <see cref="CompositeKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        void SetValue<T>(CompositeKey key, T value);

        /// <summary>
        /// Sets (adds or overrides) the cache value for the specified <see cref="Type"/> and <see cref="IPrimaryKey"/> and returns <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to set.</param>
        /// <param name="value">The value to set.</param>
        /// <returns>The <paramref name="value"/>.</returns>
        public T? SetAndReturnValue<T>(CompositeKey key, T? value)
        {
            if (value != null)
                SetValue(key, value);

            return value;
        }

        /// <summary>
        /// Sets (adds or overrides) the cache value for the specified <see cref="Type"/> (where <see cref="IPrimaryKey"/> or <see cref="IIdentifier"/>) and returns <paramref name="value"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="value">The value to set.</param>
        /// <returns>The <paramref name="value"/>.</returns>
        public T? SetAndReturnValue<T>(T? value) where T : class
        {
            if (value != null)
            {
                if (value is IPrimaryKey ipk)
                    return SetAndReturnValue(ipk.PrimaryKey, value);
                else if (value is IIdentifier iid)
                    return SetAndReturnValue(new CompositeKey(iid.Id), value);
            }

            return value;
        }

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <see cref="CompositeKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns><c>true</c> where found and removed; otherwise, <c>false</c>.</returns>
        bool Remove<T>(CompositeKey key);

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <see cref="IPrimaryKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="key">The key of the value to remove.</param>
        /// <returns><c>true</c> where found and removed; otherwise, <c>false</c>.</returns>
        public bool Remove<T>(IPrimaryKey key) => Remove<T>((key ?? throw new ArgumentNullException(nameof(key))).PrimaryKey);

        /// <summary>
        /// Removes the cached value associated with the specified <see cref="Type"/> and <see cref="IPrimaryKey"/>.
        /// </summary>
        /// <typeparam name="T">The value <see cref="Type"/>.</typeparam>
        /// <param name="identifier">The identifier of the value to remove.</param>
        /// <returns><c>true</c> where found and removed; otherwise, <c>false</c>.</returns>
        public bool Remove<T>(IIdentifier identifier) => Remove<T>(new CompositeKey((identifier ?? throw new ArgumentNullException(nameof(identifier))).Id));

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