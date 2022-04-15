// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents an <see cref="EntityBase"/> <see cref="IPrimaryKeyCollection{T}"/> class.
    /// </summary>
    /// <typeparam name="TEntity">The <see cref="EntityBase"/> <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="EntityBase"/> collection <see cref="Type"/> itself.</typeparam>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class PrimaryKeyBaseCollection<TEntity, TSelf> : EntityBaseCollection<TEntity, TSelf>, IPrimaryKeyCollection<TEntity>
        where TEntity : EntityBase, IPrimaryKey
        where TSelf : EntityBaseCollection<TEntity, TSelf>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryKeyBaseCollection{TEntity, TSelf}" /> class.
        /// </summary>
        protected PrimaryKeyBaseCollection() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimaryKeyBaseCollection{TEntity, TSelf}" /> class.
        /// </summary>
        /// <param name="collection">The entities to add.</param>
        protected PrimaryKeyBaseCollection(IEnumerable<TEntity> collection) : base(collection) { }

         /// <summary>
        /// Gets the first item by the specified primary <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns>The first item where found; otherwise, <c>default</c>. Where the underlying entity item does not implement <see cref="IPrimaryKey"/> this will always return <c>null</c>.</returns>
        public TEntity? GetByKey(CompositeKey key) => Items.Where(x => x is IPrimaryKey pk && key.Equals(pk.PrimaryKey)).FirstOrDefault();

        /// <summary>
        /// Gets the first item by the <paramref name="args"/> that represent the primary <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        /// <returns>The first item where found; otherwise, <c>default</c>. Where the underlying entity item does not implement <see cref="IPrimaryKey"/> this will always return <c>null</c>.</returns>
        public TEntity? GetByKey(params object?[] args) => GetByKey(new CompositeKey(args));

        /// <inheritdoc/>
        public bool IsAnyDuplicates() => Count != Math.Min(1, this.Count(pk => pk == null)) + this.Where(pk => pk != null).Select(pk => pk.PrimaryKey).Distinct(CompositeKeyComparer.Default).Count();

        /// <inheritdoc/>
        public void RemoveByKey(CompositeKey key) => this.Where(pk => pk.PrimaryKey == key).ToList().ForEach(pk => Remove(pk));

        /// <inheritdoc/>
        public void RemoveByKey(params object?[] args) => RemoveByKey(new CompositeKey(args));

        /// <inheritdoc/>
        public override bool Equals(object? obj) => (obj is TSelf other) && Equals(other);

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(PrimaryKeyBaseCollection<TEntity, TSelf>? a, PrimaryKeyBaseCollection<TEntity, TSelf>? b) => Equals(a, b);

        /// <summary>
        /// Compares two values for non-equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(PrimaryKeyBaseCollection<TEntity, TSelf>? a, PrimaryKeyBaseCollection<TEntity, TSelf>? b) => !Equals(a, b);

        /// <summary>
        /// Returns a hash code for the <see cref="EntityBaseCollection{TEntity, TSelf}"/>.
        /// </summary>
        /// <returns>A hash code for the <see cref="EntityBaseCollection{TEntity, TSelf}"/>.</returns>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var item in this)
            {
                hash.Add(item);
            }

            return hash.ToHashCode();
        }
    }
}