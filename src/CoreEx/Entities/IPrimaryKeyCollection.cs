// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="IPrimaryKey"/> <see cref="IKeyedCollection"/> capabilities.
    /// </summary>
    /// <typeparam name="T">The collection item <see cref="System.Type"/>.</typeparam>
    public interface IPrimaryKeyCollection<T> : IKeyedCollection, ICollection<T> where T : IPrimaryKey
    {
        /// <summary>
        /// Gets the first item using the specified primary <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The primary <see cref="CompositeKey"/>.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        object? IKeyedCollection.GetByKey(CompositeKey key) => GetByKey(key);

        /// <summary>
        /// Gets the first item using the <paramref name="args"/> which represent the primary <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        object? IKeyedCollection.GetByKey(params object?[] args) => GetByKey(args);

        /// <summary>
        /// Gets the first item using the specified primary <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The primary <see cref="CompositeKey"/>.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        new T? GetByKey(CompositeKey key);

        /// <summary>
        /// Gets the first item using the <paramref name="args"/> which represent the primary <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        new T? GetByKey(params object?[] args);
    }
}