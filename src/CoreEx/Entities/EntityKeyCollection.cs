// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;
using System.Linq;
using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents an <see cref="IEntityKey"/> collection class.
    /// </summary>
    /// <typeparam name="T">The <see cref="IEntityKey"/> item <see cref="System.Type"/>.</typeparam>
    /// <remarks>This class is underpinned by a <see cref="List{T}"/> and does <i>not</i> manage/guarantee uniqueness.</remarks>
    public class EntityKeyCollection<T> : List<T>, IEntityKeyCollection<T> where T : IEntityKey
    {
        /// <inheritdoc/>
        public bool ContainsKey(CompositeKey key) => this.Any(x => key.Equals(x.EntityKey));

        /// <inheritdoc/>
        public bool ContainsKey(params object?[] args) => ContainsKey(new CompositeKey(args));

        /// <inheritdoc/>
        public T? GetByKey(CompositeKey key) => this.Where(x => key.Equals(x.EntityKey)).FirstOrDefault();

        /// <inheritdoc/>
        public T? GetByKey(params object?[] args) => GetByKey(new CompositeKey(args));

        /// <inheritdoc/>
        public bool IsAnyDuplicates() => Count != Math.Min(1, this.Count(x => x == null)) + this.Where(x => x != null).Select(x => x.EntityKey).Distinct(CompositeKeyComparer.Default).Count();

        /// <inheritdoc/>
        public void RemoveByKey(CompositeKey key) => this.Where(x => x.EntityKey == key).ToList().ForEach(pk => Remove(pk));

        /// <inheritdoc/>
        public void RemoveByKey(params object?[] args) => RemoveByKey(new CompositeKey(args));
    }
}