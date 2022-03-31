// Copyright (c) Avanade. Licensed under the MIT License. See https://github.com/Avanade/CoreEx

using Microsoft.AspNetCore.Http;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CoreEx.Entities
{
    /// <summary>
    /// Represents a basic <see cref="ICollectionResult{TEntity}"/> class (not <see cref="EntityBaseCollection{TEntity, TSelf}"/>) with a <see cref="PagingResult"/> and underlying <see cref="Collection"/>.
    /// </summary>
    /// <typeparam name="TColl">The result collection <see cref="Type"/>.</typeparam>
    /// <typeparam name="TItem">The underlying entity <see cref="Type"/>.</typeparam>
    /// <remarks>Generally an <see cref="CollectionResult{TColl, TItem}"/> is not intended for serialized <see cref="HttpResponse"/>; the underlying <see cref="Collection"/> is serialized with the <see cref="Paging"/> returned as <see cref="HttpResponse.Headers"/>.</remarks>
    [System.Diagnostics.DebuggerStepThrough]
    public abstract class CollectionResult<TColl, TItem> : ICollectionResult<TColl, TItem>, IPagingResult
        where TColl : List<TItem>, new()
        where TItem : class
    {
        private TColl? _collection;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionResult{TColl, TEntity}"/> class.
        /// </summary>
        public CollectionResult() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CollectionResult{TColl, TEntity}"/> class with <paramref name="paging"/>.
        /// </summary>
        /// <param name="paging">Defaults the <see cref="Paging"/> to the requesting <see cref="PagingArgs"/>.</param>
        protected CollectionResult(PagingArgs paging)
        {
            if (paging != null)
                Paging = new PagingResult(paging);
        }

        /// <inheritdoc/>
        [JsonPropertyName("collection")]
        public TColl Collection
        {
            get => _collection ??= new TColl();
            set => _collection = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <inheritdoc/>
        [JsonPropertyName("paging")]
        public PagingResult? Paging { get; set; }

        /// <summary>
        /// Gets or sets the item <see cref="Type"/>.
        /// </summary>
        Type ICollectionResult.ItemType { get; } = typeof(TItem);

        /// <summary>
        /// Gets the underlying <see cref="ICollection"/>.
        /// </summary>
        ICollection? ICollectionResult.Collection => _collection;

        /// <summary>
        /// Gets the underlying <see cref="ICollection{TEntity}"/>.
        /// </summary>
        ICollection<TItem>? ICollectionResult<TItem>.Collection => _collection;
    }
}