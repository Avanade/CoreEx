// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents the core <see cref="EntityBase"/> collection capabilities.
    /// </summary>
    public interface IEntityBaseCollection : ICollection, ICleanUp, IInitial
    {
        /// <summary>
        /// Adds the items of the specified collection to the end of the current collection.
        /// </summary>
        /// <param name="collection">The collection containing the items to add.</param>
        void AddRange(IEnumerable collection);

        /// <summary>
        /// Gets the item by the specified primary <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The primary <see cref="CompositeKey"/>.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        object? GetByPrimaryKey(CompositeKey key);

        /// <summary>
        /// Gets the first item by the <paramref name="args"/> that represent the primary <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        object? GetByPrimaryKey(params object?[] args);
    }
}