// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Provides the typed <see cref="ICompositeKeyCollection"/> capabilities.
    /// </summary>
    /// <typeparam name="T">The collection item <see cref="System.Type"/>.</typeparam>
    public interface ICompositeKeyCollection<T> : ICompositeKeyCollection, ICollection<T>
    {
        /// <inheritdoc/>
        object? ICompositeKeyCollection.GetByKey(CompositeKey key) => GetByKey(key);

        /// <summary>
        /// Gets the first item using the specified primary <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        new T? GetByKey(CompositeKey key);
    }
}