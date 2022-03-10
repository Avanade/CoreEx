// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using System;
using System.Collections;
using System.Collections.Generic;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a basic <see cref="ICollectionResult{TEntity}"/> class (not <see cref="EntityBaseCollection{TEntity, TSelf}"/>) with a <see cref="PagingResult"/> and corresponding <see cref="Result"/> collection.
    /// </summary>
    /// <typeparam name="TColl">The result collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The underlying entity <see cref="Type"/>.</typeparam>
    public abstract class CollectionResult<TColl, TItem> : ICollectionResult<TColl, TItem>, IPagingResult
        where TColl : List<TItem>, new()
        where TItem : class
    {
        private TColl? _result;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionResult{TColl, TEntity}"/> class.
        /// </summary>
        /// <param name="paging">Defaults the <see cref="Paging"/> to the requesting <see cref="PagingArgs"/>.</param>
        protected CollectionResult(PagingArgs? paging = null)
        {
            if (paging != null)
                Paging = new PagingResult(paging);
        }

        /// <inheritdoc/>
        public TColl Result
        {
            get => _result ??= new TColl();
            set => _result = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc/>
        public PagingResult? Paging { get; set; }

        /// <summary>
        /// Gets or sets the item <see cref="Type"/>.
        /// </summary>
        public Type ItemType { get; } = typeof(TItem);

        /// <summary>
        /// Gets the underlying <see cref="ICollection"/>.
        /// </summary>
        ICollection? ICollectionResult.Collection => _result;

        /// <summary>
        /// Gets the underlying <see cref="ICollection{TEntity}"/>.
        /// </summary>
        ICollection<TItem>? ICollectionResult<TItem>.Collection => _result;
    }
}