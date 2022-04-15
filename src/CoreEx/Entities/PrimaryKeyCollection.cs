// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System.Collections.Generic;
using System.Linq;
using System;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents an <see cref="IPrimaryKey"/> collection class.
    /// </summary>
    /// <typeparam name="T">The item <see cref="System.Type"/>.</typeparam>
    /// <remarks>This class is underpinned by a <see cref="List{T}"/> and does <i>not</i> manage/guarantee uniqueness.</remarks>
    public class PrimaryKeyCollection<T> : List<T>, IPrimaryKeyCollection<T> where T : IPrimaryKey
    {
        /// <inheritdoc/>
        public T? GetByKey(CompositeKey key) => this.Where(pk => key.Equals(pk.PrimaryKey)).FirstOrDefault();

        /// <inheritdoc/>
        public T? GetByKey(params object?[] args) => GetByKey(new CompositeKey(args));

        /// <inheritdoc/>
        public bool IsAnyDuplicates() => Count != Math.Min(1, this.Count(pk => pk == null)) + this.Where(pk => pk != null).Select(pk => pk.PrimaryKey).Distinct(CompositeKeyComparer.Default).Count();

        /// <inheritdoc/>
        public void RemoveByKey(CompositeKey key) => this.Where(pk => pk.PrimaryKey == key).ToList().ForEach(pk => Remove(pk));

        /// <inheritdoc/>
        public void RemoveByKey(params object?[] args) => RemoveByKey(new CompositeKey(args));
    }
}