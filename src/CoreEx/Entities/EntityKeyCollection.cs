// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;
using System.Linq;
using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents an <see cref="ICompositeKeyCollection"/> with <see cref="IEntityKey"/>-based items.
    /// </summary>
    /// <typeparam name="T">The <see cref="IEntityKey"/> item <see cref="System.Type"/>.</typeparam>
    /// <remarks>This class is underpinned by a <see cref="List{T}"/> and does <i>not</i> manage/guarantee uniqueness.</remarks>
    public class EntityKeyCollection<T> : List<T>, ICompositeKeyCollection<T> where T : IEntityKey
    {
        /// <inheritdoc/>
        public bool ContainsKey(CompositeKey key) => this.Any(x => key.Equals(x.EntityKey));

        /// <summary>
        /// Indicates whether an item with the specified <paramref name="keys"/> exists.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <returns><c>true</c> where the item exists; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(params object?[] keys) => ContainsKey(new CompositeKey(keys));

        /// <inheritdoc/>
        public T? GetByKey(CompositeKey key) => this.Where(x => key.Equals(x.EntityKey)).FirstOrDefault();

        /// <summary>
        /// Gets the first item with the specified <paramref name="keys"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        /// <returns>The item where found; otherwise, <c>null</c>.</returns>
        public T? GetByKey(params object?[] keys) => GetByKey(new CompositeKey(keys));

        /// <inheritdoc/>
        public bool IsAnyDuplicates() => Count != Math.Min(1, this.Count(x => x == null)) + this.Where(x => x != null).Select(x => x.EntityKey).Distinct(CompositeKeyComparer.Default).Count();

        /// <inheritdoc/>
        public void RemoveByKey(CompositeKey key) => this.Where(x => x.EntityKey == key).ToList().ForEach(pk => Remove(pk));

        /// <summary>
        /// Removes all items with the specified primary <paramref name="keys"/>.
        /// </summary>
        /// <param name="keys">The key values.</param>
        void RemoveByKey(params object?[] keys) => RemoveByKey(new CompositeKey(keys));
    }
}