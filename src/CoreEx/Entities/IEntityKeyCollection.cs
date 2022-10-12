// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the <see cref="IEntityKey.EntityKey"/> <see cref="ICompositeKeyedCollection"/> capabilities.
    /// </summary>
    /// <typeparam name="T">The <see cref="IEntityKey"/> collection item <see cref="System.Type"/>.</typeparam>
    public interface IEntityKeyCollection<T> : ICompositeKeyedCollection, ICollection<T> where T : IEntityKey
    {
        /// <inheritdoc/>
        object? ICompositeKeyedCollection.GetByKey(CompositeKey key) => GetByKey(key);

        /// <inheritdoc/>
        object? ICompositeKeyedCollection.GetByKey(params object?[] args) => GetByKey(args);

        /// <summary>
        /// Gets the first item using the specified primary <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="IEntityKey.EntityKey"/>.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        new T? GetByKey(CompositeKey key);

        /// <summary>
        /// Gets the first item using the <paramref name="args"/> which represent the <see cref="IEntityKey.EntityKey"/>.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        new T? GetByKey(params object?[] args);
    }
}