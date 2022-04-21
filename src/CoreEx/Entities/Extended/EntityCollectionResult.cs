// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreEx.Entities.Extended
{
    /// <summary>
    /// Represents an <see cref="EntityBaseCollection{TEntity, TSelf}"/> class with a <see cref="PagingResult"/> and underlying <see cref="Collection"/>.
    /// </summary>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEntity">The entity item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The entity <see cref="Type"/> itself.</typeparam>
    /// <remarks>Generally an <see cref="EntityCollectionResult{TColl, TEntity, TSelf}"/> is not intended for serialized <see cref="HttpResponse"/>; the underlying <see cref="Collection"/> is serialized with the <see cref="Paging"/> returned as <see cref="HttpResponse.Headers"/>.</remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityCollectionResult<TColl, TEntity, TSelf> : EntityBase<EntityCollectionResult<TColl, TEntity, TSelf>>, ICollectionResult<TColl, TEntity>, IPagingResult, ICopyFrom<EntityCollectionResult<TColl, TEntity, TSelf>>
        where TColl : EntityBaseCollection<TEntity, TColl>, new()
        where TEntity : EntityBase<TEntity>
        where TSelf : EntityCollectionResult<TColl, TEntity, TSelf>, new()
    {
        private PagingResult? _paging;
        private TColl? _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityCollectionResult{TColl, TEntity, TSelf}"/> class.
        /// </summary>
        /// <param name="paging">Defaults the <see cref="Paging"/> to the requesting <see cref="PagingArgs"/>.</param>
        protected EntityCollectionResult(PagingArgs? paging = null)
        {
            if (paging != null)
                _paging = new PagingResult(paging);
        }

        /// <summary>
        /// Gets or sets the underlying collection.
        /// </summary>
        public TColl Collection { get => _collection ??= new TColl(); set => SetValue(ref _collection, value); }

        /// <summary>
        /// Gets or sets the <see cref="PagingResult"/>.
        /// </summary>
        /// <remarks>Where this value is <c>null</c> it indicates that the paging was unable to be determined.</remarks>
        public PagingResult? Paging { get => _paging; set => SetValue(ref _paging, value); }

        /// <summary>
        /// Gets the item <see cref="Type"/>.
        /// </summary>
        Type ICollectionResult.ItemType => typeof(TEntity);

        /// <summary>
        /// Gets the underlying <see cref="ICollection"/>.
        /// </summary>
        ICollection? ICollectionResult.Collection => Collection;

        /// <summary>
        /// Gets the underlying <see cref="ICollection{TEntity}"/>.
        /// </summary>
        ICollection<TEntity>? ICollectionResult<TEntity>.Collection => Collection;

        /// <inheritdoc/>
        public override bool Equals(EntityCollectionResult<TColl, TEntity, TSelf>? other) => ReferenceEquals(this, other) || (other != null && base.Equals(other)
            && Equals(Collection, other!.Collection)
            && Equals(Paging, other.Paging));

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Collection);
            hash.Add(Paging);
            return base.GetHashCode() ^ hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override object Clone()
        {
            var clone = new TSelf();
            clone.CopyFrom(this);
            return clone;
        }

        /// <inheritdoc/>
        public override void CopyFrom(EntityCollectionResult<TColl, TEntity, TSelf> from)
        {
            base.CopyFrom(from);
            Collection = CopyOrClone(from.Collection, Collection)!;
            Paging = from.Paging;
        }

        /// <inheritdoc/>
        protected override void OnApplyAction(EntityAction action)
        {
            base.OnApplyAction(action);
            Collection = ApplyAction(Collection, action);
            Paging = ApplyAction(Paging, action);
        }

        /// <inheritdoc/>
        public override bool IsInitial => base.IsInitial
            && Cleaner.IsDefault(Collection)
            && Cleaner.IsDefault(Paging);
    }
}