// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents an <see cref="EntityBaseCollection{TEntity, TSelf}"/> class with a <see cref="PagingResult"/> and corresponding <see cref="Result"/> collection.
    /// </summary>
    /// <typeparam name="TColl">The collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TEntity">The entity item <see cref="Type"/>.</typeparam>
    /// <typeparam name="TSelf">The entity <see cref="Type"/> itself.</typeparam>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class EntityCollectionResult<TColl, TEntity, TSelf> : EntityBase<EntityCollectionResult<TColl, TEntity, TSelf>>, ICollectionResult<TColl, TEntity>, IPagingResult, ICopyFrom<EntityCollectionResult<TColl, TEntity, TSelf>>
        where TColl : EntityBaseCollection<TEntity, TColl>, new()
        where TEntity : EntityBase<TEntity>
        where TSelf : EntityCollectionResult<TColl, TEntity, TSelf>, new()
    {
        private PagingResult? _paging;
        private TColl? _result;

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
        /// Gets or sets the result.
        /// </summary>
        public TColl Result { get => _result ??= new TColl(); set => SetValue(ref _result, value); }

        /// <summary>
        /// Gets or sets the <see cref="PagingResult"/>.
        /// </summary>
        /// <remarks>Where this value is <c>null</c> it indicates that the paging was unable to be determined.</remarks>
        public PagingResult? Paging { get => _paging; set => SetValue(ref _paging, value); }

        /// <summary>
        /// Gets or sets the item <see cref="Type"/>.
        /// </summary>
        public Type ItemType => typeof(TEntity);

        /// <summary>
        /// Gets the underlying <see cref="ICollection"/>.
        /// </summary>
        ICollection? ICollectionResult.Collection => Result;

        /// <summary>
        /// Gets the underlying <see cref="ICollection{TEntity}"/>.
        /// </summary>
        ICollection<TEntity>? ICollectionResult<TEntity>.Collection => Result;

        /// <inheritdoc/>
        public override bool Equals(EntityCollectionResult<TColl, TEntity, TSelf> other) => ReferenceEquals(this, other) || (other != null && base.Equals(other)
            && Equals(Result, other!.Result)
            && Equals(Paging, other.Paging));

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Result);
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
            Result = CopyOrClone(from.Result, Result)!;
            Paging = from.Paging;
        }

        /// <inheritdoc/>
        protected override void OnApplyAction(EntityAction action)
        {
            base.OnApplyAction(action);
            Result = ApplyAction(Result, action);
            Paging = ApplyAction(Paging, action);
        }

        /// <inheritdoc/>
        public override bool IsInitial => base.IsInitial
            && Cleaner.IsDefault(Result)
            && Cleaner.IsDefault(Paging);
    }
}