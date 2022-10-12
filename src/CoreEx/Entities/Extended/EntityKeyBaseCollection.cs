// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections.Generic;
using System.Linq;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents an <see cref="EntityBase"/> <see cref="IEntityKeyCollection{T}"/> class.
    /// </summary>
    /// <typeparam name="TEntity">The <see cref="IEntityKey"/> <see cref="EntityBase"/> <see cref="System.Type"/>.</typeparam>
    /// <typeparam name="TSelf">The <see cref="EntityBase"/> collection <see cref="Type"/> itself.</typeparam>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityKeyBaseCollection<TEntity, TSelf> : EntityBaseCollection<TEntity, TSelf>, IEntityKeyCollection<TEntity>
        where TEntity : EntityBase, IEntityKey
        where TSelf : EntityBaseCollection<TEntity, TSelf>, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EntityKeyBaseCollection{TEntity, TSelf}" /> class.
        /// </summary>
        protected EntityKeyBaseCollection() : base() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityKeyBaseCollection{TEntity, TSelf}" /> class.
        /// </summary>
        /// <param name="collection">The entities to add.</param>
        protected EntityKeyBaseCollection(IEnumerable<TEntity> collection) : base(collection) { }

        /// <inheritdoc/>
        public bool ContainsKey(CompositeKey key) => Items.Any(x => key.Equals(x.EntityKey));

        /// <inheritdoc/>
        public bool ContainsKey(params object?[] args) => ContainsKey(new CompositeKey(args));

        /// <summary>
        /// Gets the first item by the specified primary <paramref name="key"/>.
        /// </summary>
        /// <param name="key">The <see cref="CompositeKey"/>.</param>
        /// <returns>The first item where found; otherwise, <c>default</c>.</returns>
        public TEntity? GetByKey(CompositeKey key) => Items.Where(x => key.Equals(x.EntityKey)).FirstOrDefault();

        /// <summary>
        /// Gets the first item by the <paramref name="args"/> that represent the primary <see cref="CompositeKey"/>.
        /// </summary>
        /// <param name="args">The argument values for the key.</param>
        /// <returns>The first item where found; otherwise, <c>default</c>.</returns>
        public TEntity? GetByKey(params object?[] args) => GetByKey(new CompositeKey(args));

        /// <inheritdoc/>
        public bool IsAnyDuplicates() => Count != Math.Min(1, this.Count(ek => ek == null)) + this.Where(ek => ek != null).Select(ek => ek.EntityKey).Distinct(CompositeKeyComparer.Default).Count();

        /// <inheritdoc/>
        public void RemoveByKey(CompositeKey key) => this.Where(ek => ek.EntityKey == key).ToList().ForEach(ek => Remove(ek));

        /// <inheritdoc/>
        public void RemoveByKey(params object?[] args) => RemoveByKey(new CompositeKey(args));

        /// <inheritdoc/>
        public override bool Equals(object? obj) => base.Equals(obj);

        /// <inheritdoc/>
        public override int GetHashCode() => base.GetHashCode();

        /// <summary>
        /// Compares two values for equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates equal; otherwise, <c>false</c> for not equal.</returns>
        public static bool operator ==(EntityKeyBaseCollection<TEntity, TSelf>? a, EntityKeyBaseCollection<TEntity, TSelf>? b) => Equals(a, b);

        /// <summary>
        /// Compares two values for non-equality.
        /// </summary>
        /// <param name="a"><see cref="ChangeLog"/> A.</param>
        /// <param name="b"><see cref="ChangeLog"/> B.</param>
        /// <returns><c>true</c> indicates not equal; otherwise, <c>false</c> for equal.</returns>
        public static bool operator !=(EntityKeyBaseCollection<TEntity, TSelf>? a, EntityKeyBaseCollection<TEntity, TSelf>? b) => !Equals(a, b);
    }
}